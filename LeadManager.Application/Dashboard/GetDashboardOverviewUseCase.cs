using LeadManager.Application.Abstractions;

namespace LeadManager.Application.Dashboard;

public sealed class GetDashboardOverviewUseCase
{
    private readonly ILeadAnalyticsRepository _leadAnalyticsRepository;

    public GetDashboardOverviewUseCase(ILeadAnalyticsRepository leadAnalyticsRepository)
    {
        _leadAnalyticsRepository = leadAnalyticsRepository;
    }

    public Task<DashboardOverviewResponse> ExecuteAsync(CancellationToken cancellationToken = default) =>
        _leadAnalyticsRepository.GetOverviewAsync(cancellationToken);
}
