using LeadManager.Application.Auth;
using LeadManager.Application.Campaigns;
using LeadManager.Application.Dashboard;
using LeadManager.Application.Abstractions;
using LeadManager.Application.Integrations;
using LeadManager.Application.Leads;
using LeadManager.Application.Users;
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
        services.AddScoped<GetLeadHistoryUseCase>();
        services.AddScoped<UpdateLeadStatusUseCase>();
        services.AddScoped<RecalculateLeadScoreUseCase>();
        services.AddScoped<MergeLeadUseCase>();
        services.AddScoped<AuthUseCase>();
        services.AddScoped<UserManagementUseCase>();
        services.AddScoped<CampaignManagementUseCase>();
        services.AddScoped<GetDashboardOverviewUseCase>();
        services.AddScoped<SyncLeadToCrmUseCase>();
        services.AddScoped<RecordWebhookEventUseCase>();

        return services;
    }
}
