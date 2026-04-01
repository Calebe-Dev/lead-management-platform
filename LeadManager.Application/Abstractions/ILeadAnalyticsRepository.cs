using LeadManager.Application.Dashboard;

namespace LeadManager.Application.Abstractions;

public interface ILeadAnalyticsRepository
{
    Task<DashboardOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken = default);
}
