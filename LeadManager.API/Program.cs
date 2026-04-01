using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LeadManager.Application;
using LeadManager.Application.Auth;
using LeadManager.Application.Campaigns;
using LeadManager.Application.Dashboard;
using LeadManager.Application.Integrations;
using LeadManager.Application.Leads;
using LeadManager.Application.Users;
using LeadManager.Infrastructure;
using LeadManager.Infrastructure.Auth;
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
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = LoadJwtOptions(builder.Configuration);
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
    await DatabaseSchemaBootstrapper.EnsureAdvancedSchemaAsync(dbContext);
}

await EnsureSeedUsersAsync(app.Services, builder.Configuration);

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

app.MapPost("/api/auth/token", async (TokenRequest request, AuthUseCase useCase, CancellationToken cancellationToken) =>
{
    var response = await useCase.IssueTokenAsync(request, cancellationToken);
    return response is null ? Results.Unauthorized() : Results.Ok(response);
});

app.MapPost("/api/auth/refresh", async (RefreshTokenRequest request, AuthUseCase useCase, CancellationToken cancellationToken) =>
{
    var response = await useCase.RefreshAsync(request, cancellationToken);
    return response is null ? Results.Unauthorized() : Results.Ok(response);
});

app.MapPost("/api/auth/logout", async (RefreshTokenRequest request, AuthUseCase useCase, CancellationToken cancellationToken) =>
{
    await useCase.LogoutAsync(request, cancellationToken);
    return Results.NoContent();
});

var usersGroup = app.MapGroup("/api/users");
usersGroup.RequireAuthorization(policy => policy.RequireRole("admin"));
usersGroup.MapGet(string.Empty, async (UserManagementUseCase useCase, CancellationToken cancellationToken) =>
{
    var users = await useCase.ListAsync(cancellationToken);
    return Results.Ok(users);
});
usersGroup.MapPost(string.Empty, async (CreateUserCommand command, UserManagementUseCase useCase, CancellationToken cancellationToken) =>
{
    var user = await useCase.CreateAsync(command, cancellationToken);
    return Results.Created($"/api/users/{user.Id}", user);
});
usersGroup.MapDelete("/{id:guid}", async (Guid id, UserManagementUseCase useCase, CancellationToken cancellationToken) =>
{
    var deleted = await useCase.DeleteAsync(id, cancellationToken);
    return deleted ? Results.NoContent() : Results.NotFound();
});

var campaignsGroup = app.MapGroup("/api/campaigns");
campaignsGroup.RequireAuthorization(policy => policy.RequireRole("admin", "marketing"));
campaignsGroup.MapGet(string.Empty, async (CampaignManagementUseCase useCase, CancellationToken cancellationToken) =>
{
    var campaigns = await useCase.ListAsync(cancellationToken);
    return Results.Ok(campaigns);
});
campaignsGroup.MapGet("/{id:guid}", async (Guid id, CampaignManagementUseCase useCase, CancellationToken cancellationToken) =>
{
    var campaign = await useCase.GetByIdAsync(id, cancellationToken);
    return campaign is null ? Results.NotFound() : Results.Ok(campaign);
});
campaignsGroup.MapPost(string.Empty, async (UpsertCampaignCommand command, CampaignManagementUseCase useCase, CancellationToken cancellationToken) =>
{
    var campaign = await useCase.CreateAsync(command, cancellationToken);
    return Results.Created($"/api/campaigns/{campaign.Id}", campaign);
});
campaignsGroup.MapPut("/{id:guid}", async (Guid id, UpsertCampaignCommand command, CampaignManagementUseCase useCase, CancellationToken cancellationToken) =>
{
    var campaign = await useCase.UpdateAsync(id, command, cancellationToken);
    return campaign is null ? Results.NotFound() : Results.Ok(campaign);
});
campaignsGroup.MapDelete("/{id:guid}", async (Guid id, CampaignManagementUseCase useCase, CancellationToken cancellationToken) =>
{
    var deleted = await useCase.DeleteAsync(id, cancellationToken);
    return deleted ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization(policy => policy.RequireRole("admin"));

var leadsGroup = app.MapGroup("/api/leads");
leadsGroup.RequireAuthorization(policy => policy.RequireRole("admin", "marketing", "vendas"));

leadsGroup.MapPost(string.Empty, async (CreateLeadCommand command, CreateLeadUseCase useCase, CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(command, cancellationToken);
    return Results.Created($"/api/leads/{result.Id}", result);
});

leadsGroup.MapGet(string.Empty, async (
    LeadManager.Domain.Leads.LeadStatus? status,
    LeadManager.Domain.Leads.LeadTemperature? temperature,
    string? region,
    string? leadType,
    string? productInterest,
    string? assignedTo,
    string? search,
    int? minScore,
    int? maxScore,
    int? page,
    int? pageSize,
    Guid? campaignId,
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
            PageSize = !pageSize.HasValue || pageSize.Value == 0 ? 20 : pageSize.Value,
            CampaignId = campaignId
        },
        cancellationToken);

    return Results.Ok(result);
});

leadsGroup.MapGet("/{id:guid}", async (Guid id, GetLeadByIdUseCase useCase, CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(id, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

leadsGroup.MapGet("/{id:guid}/history", async (
    Guid id,
    int? page,
    int? pageSize,
    GetLeadHistoryUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(
        id,
        new LeadHistoryQuery
        {
            Page = !page.HasValue || page.Value == 0 ? 1 : page.Value,
            PageSize = !pageSize.HasValue || pageSize.Value == 0 ? 20 : pageSize.Value
        },
        cancellationToken);
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

leadsGroup.MapPost("/{id:guid}/merge", async (Guid id, MergeLeadCommand command, MergeLeadUseCase useCase, CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(id, command, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
}).RequireAuthorization(policy => policy.RequireRole("admin"));

var dashboardGroup = app.MapGroup("/api/dashboard");
dashboardGroup.RequireAuthorization(policy => policy.RequireRole("admin", "marketing", "vendas"));
dashboardGroup.MapGet("/overview", async (GetDashboardOverviewUseCase useCase, CancellationToken cancellationToken) =>
{
    var overview = await useCase.ExecuteAsync(cancellationToken);
    return Results.Ok(overview);
});

var integrationsGroup = app.MapGroup("/api/integrations");
integrationsGroup.RequireAuthorization(policy => policy.RequireRole("admin", "vendas"));
integrationsGroup.MapPost("/crm/sync/{leadId:guid}", async (Guid leadId, SyncLeadToCrmUseCase useCase, CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(leadId, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapPost("/api/integrations/webhooks/hubspot", async (JsonElement payload, RecordWebhookEventUseCase useCase, CancellationToken cancellationToken) =>
{
    await useCase.ExecuteAsync("hubspot", payload, cancellationToken);
    return Results.Accepted();
});
app.MapPost("/api/integrations/webhooks/salesforce", async (JsonElement payload, RecordWebhookEventUseCase useCase, CancellationToken cancellationToken) =>
{
    await useCase.ExecuteAsync("salesforce", payload, cancellationToken);
    return Results.Accepted();
});
app.MapPost("/api/integrations/webhooks/whatsapp", async (JsonElement payload, RecordWebhookEventUseCase useCase, CancellationToken cancellationToken) =>
{
    await useCase.ExecuteAsync("whatsapp", payload, cancellationToken);
    return Results.Accepted();
});

app.Run();

static JwtTokenOptions LoadJwtOptions(IConfiguration configuration)
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

    return new JwtTokenOptions
    {
        Issuer = issuer,
        Audience = audience,
        SigningKey = signingKey,
        ExpiresInMinutes = expiresInMinutes
    };
}

static string? NormalizeOptionalQuery(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();

static async Task EnsureSeedUsersAsync(IServiceProvider serviceProvider, IConfiguration configuration)
{
    using var scope = serviceProvider.CreateScope();
    var userRepository = scope.ServiceProvider.GetRequiredService<LeadManager.Application.Abstractions.IUserRepository>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<LeadManager.Application.Abstractions.IPasswordHasher>();
    var existingUsers = await userRepository.ListAsync();
    if (existingUsers.Count > 0)
    {
        return;
    }

    var configuredUsers = configuration.GetSection("Auth:Users").Get<List<BootstrapAuthUser>>() ?? [];
    foreach (var configuredUser in configuredUsers)
    {
        if (string.IsNullOrWhiteSpace(configuredUser.Username)
            || string.IsNullOrWhiteSpace(configuredUser.Password)
            || string.IsNullOrWhiteSpace(configuredUser.Role))
        {
            continue;
        }

        await userRepository.CreateAsync(
            new CreateUserCommand(configuredUser.Username.Trim(), string.Empty, configuredUser.Role.Trim().ToLowerInvariant()),
            passwordHasher.Hash(configuredUser.Password.Trim()),
            CancellationToken.None);
    }
}

public sealed record BootstrapAuthUser(string Username, string Password, string Role);

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
        catch (UnauthorizedAccessException exception)
        {
            await WriteProblem(context, StatusCodes.Status401Unauthorized, exception.Message);
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
