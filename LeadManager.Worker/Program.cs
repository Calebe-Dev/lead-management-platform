using LeadManager.Application;
using LeadManager.Infrastructure;
using LeadManager.Infrastructure.Outbox;
using LeadManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<OutboxBackgroundWorker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LeadManagerDbContext>();
    dbContext.Database.Migrate();
    await DatabaseSchemaBootstrapper.EnsureAdvancedSchemaAsync(dbContext);
}

await host.RunAsync();

public sealed class OutboxBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<OutboxBackgroundWorker> _logger;

    public OutboxBackgroundWorker(IServiceScopeFactory serviceScopeFactory, ILogger<OutboxBackgroundWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox background worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();
                var processed = await processor.ProcessBatchAsync(50, stoppingToken);
                if (processed > 0)
                {
                    _logger.LogInformation("Processed {Count} outbox messages.", processed);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Outbox processing failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
