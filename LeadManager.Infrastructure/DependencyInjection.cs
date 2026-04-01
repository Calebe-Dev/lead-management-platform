using LeadManager.Application.Abstractions;
using LeadManager.Infrastructure.Auditing;
using LeadManager.Infrastructure.Auth;
using LeadManager.Infrastructure.Caching;
using LeadManager.Infrastructure.Integrations;
using LeadManager.Infrastructure.LeadDistribution;
using LeadManager.Infrastructure.Outbox;
using LeadManager.Infrastructure.Persistence;
using LeadManager.Infrastructure.Scoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LeadManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LeadManagerDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'LeadManagerDatabase' was not configured.");
        }

        services.AddDbContext<LeadManagerDbContext>(options => options.UseNpgsql(connectionString));
        services.Configure<LeadDistributionOptions>(configuration.GetSection("LeadDistribution"));
        services.Configure<JwtTokenOptions>(configuration.GetSection("Jwt"));
        services.Configure<LeadScoringOptions>(configuration.GetSection("LeadScoring"));
        services.Configure<IntegrationOptions>(configuration.GetSection("Integrations"));
        services.Configure<MongoAuditOptions>(options =>
        {
            options.Enabled = !string.IsNullOrWhiteSpace(configuration.GetConnectionString("Mongo"));
            options.ConnectionString = configuration.GetConnectionString("Mongo") ?? string.Empty;
            options.Database = configuration["Mongo:Database"] ?? "lead_manager_audit";
        });
        AddCaching(services, configuration);
        services.AddHttpClient();

        services.AddScoped<ILeadRepository, EfLeadRepository>();
        services.AddScoped<ILeadHistoryRepository, EfLeadHistoryRepository>();
        services.AddScoped<IRoundRobinStateRepository, EfRoundRobinStateRepository>();
        services.AddScoped<IAssignmentRepository, EfAssignmentRepository>();
        services.AddScoped<ILeadAssignmentService, RuleBasedLeadAssignmentService>();
        services.AddScoped<ILeadListCache, RedisLeadListCache>();
        services.AddScoped<ICampaignRepository, EfCampaignRepository>();
        services.AddScoped<EfUserRepository>();
        services.AddScoped<IUserRepository>(provider => provider.GetRequiredService<EfUserRepository>());
        services.AddScoped<IUserPasswordRepository>(provider => provider.GetRequiredService<EfUserRepository>());
        services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();
        services.AddScoped<ILeadAnalyticsRepository, EfLeadAnalyticsRepository>();
        services.AddScoped<IOutboxRepository, EfOutboxRepository>();
        services.AddSingleton<IAuditTrailRepository, MongoAuditTrailRepository>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddSingleton<ILeadScoringService, LeadScoringService>();
        services.AddSingleton<ICrmIntegrationService, HttpCrmIntegrationService>();
        services.AddSingleton<IWhatsAppIntegrationService, HttpWhatsAppIntegrationService>();
        services.AddScoped<OutboxProcessor>();

        return services;
    }

    private static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            throw new InvalidOperationException("Connection string 'Redis' was not configured.");
        }

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "lead-manager:";
        });
    }
}
