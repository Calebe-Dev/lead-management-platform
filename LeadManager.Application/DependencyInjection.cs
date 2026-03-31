using LeadManager.Application.Leads;
using LeadManager.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace LeadManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ILeadListCache, NoOpLeadListCache>();
        services.AddScoped<CreateLeadUseCase>();
        services.AddScoped<ListLeadsUseCase>();
        services.AddScoped<GetLeadByIdUseCase>();
        services.AddScoped<UpdateLeadStatusUseCase>();
        services.AddScoped<RecalculateLeadScoreUseCase>();

        return services;
    }
}
