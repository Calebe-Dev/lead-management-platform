using LeadManager.Application.Abstractions;
using LeadManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

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
        services.AddScoped<ILeadRepository, EfLeadRepository>();
        return services;
    }
}
