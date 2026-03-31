using LeadManager.Application.Abstractions;
using LeadManager.Infrastructure.Caching;
using LeadManager.Infrastructure.LeadDistribution;
using LeadManager.Infrastructure.Persistence;
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
        AddCaching(services, configuration);

        services.AddScoped<ILeadRepository, EfLeadRepository>();
        services.AddScoped<ILeadHistoryRepository, EfLeadHistoryRepository>();
        services.AddScoped<IRoundRobinStateRepository, EfRoundRobinStateRepository>();
        services.AddScoped<ILeadAssignmentService, RuleBasedLeadAssignmentService>();
        services.AddScoped<ILeadListCache, RedisLeadListCache>();

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
