using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LeadManager.Application;
using LeadManager.Application.Leads;
using LeadManager.Domain.Leads;
using LeadManager.Infrastructure;
using LeadManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = JwtOptions.FromConfiguration(builder.Configuration);
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<LeadManagerDbContext>();
    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}
app.UseMiddleware<ApiExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/token", (TokenRequest request, IConfiguration configuration) =>
{
    ArgumentNullException.ThrowIfNull(request);
    var username = request.Username?.Trim() ?? string.Empty;
    var password = request.Password?.Trim() ?? string.Empty;

    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
        throw new ArgumentException("Username and password are required.");
    }

    var authOptions = AuthOptions.FromConfiguration(configuration);
    var user = authOptions.Users
        .FirstOrDefault(candidate => candidate.Username == username && candidate.Password == password);

    if (user is null)
    {
        return Results.Unauthorized();
    }

    var jwtOptions = JwtOptions.FromConfiguration(configuration);
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var expiresAtUtc = DateTime.UtcNow.AddMinutes(jwtOptions.ExpiresInMinutes);

    var token = new JwtSecurityToken(
        issuer: jwtOptions.Issuer,
        audience: jwtOptions.Audience,
        claims:
        [
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        ],
        expires: expiresAtUtc,
        signingCredentials: credentials);

    var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new TokenResponse(accessToken, expiresAtUtc));
});

var leadsGroup = app.MapGroup("/api/leads");
leadsGroup.RequireAuthorization(policy => policy.RequireRole("admin", "marketing", "vendas"));

leadsGroup.MapPost(string.Empty, async (CreateLeadCommand command, CreateLeadUseCase useCase, CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(command, cancellationToken);
    return Results.Created($"/api/leads/{result.Id}", result);
});

leadsGroup.MapGet(string.Empty, async (
    LeadStatus? status,
    LeadTemperature? temperature,
    string? region,
    string? leadType,
    string? productInterest,
    string? assignedTo,
    string? search,
    int? minScore,
    int? maxScore,
    int? page,
    int? pageSize,
    ListLeadsUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(
        new ListLeadsQuery
        {
            Status = status,
            Temperature = temperature,
            Region = NormalizeOptionalQuery(region),
            LeadType = NormalizeOptionalQuery(leadType),
            ProductInterest = NormalizeOptionalQuery(productInterest),
            AssignedTo = NormalizeOptionalQuery(assignedTo),
            Search = NormalizeOptionalQuery(search),
            MinScore = minScore,
            MaxScore = maxScore,
            Page = !page.HasValue || page.Value == 0 ? 1 : page.Value,
            PageSize = !pageSize.HasValue || pageSize.Value == 0 ? 20 : pageSize.Value
        },
        cancellationToken);

    return Results.Ok(result);
});

leadsGroup.MapGet("/{id:guid}", async (Guid id, GetLeadByIdUseCase useCase, CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(id, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

leadsGroup.MapGet("/{id:guid}/history", async (Guid id, GetLeadHistoryUseCase useCase, CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(id, cancellationToken);
    return Results.Ok(result);
});

leadsGroup.MapPatch("/{id:guid}/status", async (Guid id, UpdateLeadStatusCommand command, UpdateLeadStatusUseCase useCase, CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(id, command, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
}).RequireAuthorization(policy => policy.RequireRole("admin", "vendas"));

leadsGroup.MapPost("/{id:guid}/score", async (Guid id, RecalculateLeadScoreUseCase useCase, CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(id, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
}).RequireAuthorization(policy => policy.RequireRole("admin", "vendas"));

app.Run();

static string? NormalizeOptionalQuery(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();

public sealed record TokenRequest(string Username, string Password);
public sealed record TokenResponse(string AccessToken, DateTime ExpiresAtUtc);
public sealed record AuthUser(string Username, string Password, string Role);

public sealed record JwtOptions(string Issuer, string Audience, string SigningKey, int ExpiresInMinutes)
{
    public static JwtOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("Jwt");
        var issuer = section["Issuer"]?.Trim() ?? string.Empty;
        var audience = section["Audience"]?.Trim() ?? string.Empty;
        var signingKey = section["SigningKey"]?.Trim() ?? string.Empty;
        var expiresInMinutesText = section["ExpiresInMinutes"]?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("JWT issuer configuration is required.");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("JWT audience configuration is required.");
        }

        if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
        {
            throw new InvalidOperationException("JWT signing key must be configured with at least 32 characters.");
        }

        if (!int.TryParse(expiresInMinutesText, out var expiresInMinutes) || expiresInMinutes <= 0)
        {
            throw new InvalidOperationException("JWT expiration configuration must be a positive integer.");
        }

        return new JwtOptions(issuer, audience, signingKey, expiresInMinutes);
    }
}

public sealed record AuthOptions(IReadOnlyCollection<AuthUser> Users)
{
    public static AuthOptions FromConfiguration(IConfiguration configuration)
    {
        var users = configuration.GetSection("Auth:Users").Get<List<AuthUser>>() ?? [];
        if (users.Count == 0)
        {
            throw new InvalidOperationException("Authentication users were not configured.");
        }

        var allowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "admin", "marketing", "vendas" };
        foreach (var user in users)
        {
            if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password) || string.IsNullOrWhiteSpace(user.Role))
            {
                throw new InvalidOperationException("Each auth user must define username, password and role.");
            }

            if (!allowedRoles.Contains(user.Role))
            {
                throw new InvalidOperationException($"Unsupported role '{user.Role}' configured.");
            }
        }

        var duplicateUsernames = users
            .GroupBy(user => user.Username, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateUsernames.Length > 0)
        {
            throw new InvalidOperationException($"Duplicate auth users configured: {string.Join(", ", duplicateUsernames)}.");
        }

        return new AuthOptions(users);
    }
}

public sealed class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ApiExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DuplicateLeadException exception)
        {
            await WriteProblem(context, StatusCodes.Status409Conflict, exception.Message);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            await WriteProblem(context, StatusCodes.Status400BadRequest, exception.Message);
        }
        catch (ArgumentException exception)
        {
            await WriteProblem(context, StatusCodes.Status400BadRequest, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            await WriteProblem(context, StatusCodes.Status409Conflict, exception.Message);
        }
    }

    private static Task WriteProblem(HttpContext context, int statusCode, string detail)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new
        {
            title = "Request could not be processed.",
            status = statusCode,
            detail
        });
    }
}

public partial class Program;
