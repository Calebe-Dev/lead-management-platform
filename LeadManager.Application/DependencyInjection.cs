using LeadManager.Application.Leads;
using Microsoft.Extensions.DependencyInjection;

namespace LeadManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateLeadUseCase>();
        services.AddScoped<ListLeadsUseCase>();
        return services;
    }
}
