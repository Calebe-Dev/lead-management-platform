using LeadManager.Application;
using LeadManager.Application.Leads;
using LeadManager.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

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

app.Run();
