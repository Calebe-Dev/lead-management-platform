using LeadManager.Application.Abstractions;
using LeadManager.Application.Dashboard;
using LeadManager.Domain.Leads;
using Microsoft.EntityFrameworkCore;

namespace LeadManager.Infrastructure.Persistence;

public sealed class EfLeadAnalyticsRepository : ILeadAnalyticsRepository
{
    private readonly LeadManagerDbContext _dbContext;

    public EfLeadAnalyticsRepository(LeadManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var totalLeads = await _dbContext.Leads.CountAsync(cancellationToken);
        var groupedByStatus = await _dbContext.Leads
            .GroupBy(x => x.Status)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var groupedByTemperature = await _dbContext.Leads
            .GroupBy(x => x.Temperature)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var groupedBySource = await _dbContext.Leads
            .GroupBy(x => x.Source)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var averageScore = totalLeads == 0
            ? 0
            : await _dbContext.Leads.AverageAsync(x => (double)x.Score, cancellationToken);

        var converted = groupedByStatus.FirstOrDefault(x => x.Key == (int)LeadStatus.Converted)?.Count ?? 0;
        var conversionRate = totalLeads == 0 ? 0 : (double)converted / totalLeads;

        return new DashboardOverviewResponse(
            totalLeads,
            groupedByStatus.FirstOrDefault(x => x.Key == (int)LeadStatus.New)?.Count ?? 0,
            groupedByStatus.FirstOrDefault(x => x.Key == (int)LeadStatus.InService)?.Count ?? 0,
            groupedByStatus.FirstOrDefault(x => x.Key == (int)LeadStatus.Qualified)?.Count ?? 0,
            groupedByStatus.FirstOrDefault(x => x.Key == (int)LeadStatus.Converted)?.Count ?? 0,
            groupedByStatus.FirstOrDefault(x => x.Key == (int)LeadStatus.Lost)?.Count ?? 0,
            Math.Round(averageScore, 2),
            Math.Round(conversionRate * 100, 2),
            groupedByTemperature
                .Select(x => new DashboardDimensionCount(((LeadTemperature)x.Key).ToString(), x.Count))
                .OrderByDescending(x => x.Count)
                .ToArray(),
            groupedBySource
                .Select(x => new DashboardDimensionCount(x.Key, x.Count))
                .OrderByDescending(x => x.Count)
                .ToArray());
    }
}
