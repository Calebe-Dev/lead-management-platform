using LeadManager.Application.Abstractions;
using LeadManager.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace LeadManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ILeadRepository, InMemoryLeadRepository>();
        return services;
    }
}
