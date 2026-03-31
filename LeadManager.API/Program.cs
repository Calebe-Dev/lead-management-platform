using LeadManager.Application;
using LeadManager.Application.Leads;
using LeadManager.Infrastructure;
using LeadManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LeadManagerDbContext>();
    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/api/leads", async (CreateLeadCommand command, CreateLeadUseCase useCase, CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(command, cancellationToken);
    return Results.Created($"/api/leads/{result.Id}", result);
});

app.MapGet("/api/leads", async (ListLeadsUseCase useCase, CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(cancellationToken);
    return Results.Ok(result);
});

app.MapGet("/api/leads/{id:guid}", async (Guid id, GetLeadByIdUseCase useCase, CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(id, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapPatch("/api/leads/{id:guid}/status", async (Guid id, UpdateLeadStatusCommand command, UpdateLeadStatusUseCase useCase, CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(id, command, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapPost("/api/leads/{id:guid}/score", async (Guid id, RecalculateLeadScoreUseCase useCase, CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(id, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.Run();
