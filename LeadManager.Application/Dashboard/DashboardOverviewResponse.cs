namespace LeadManager.Application.Dashboard;

public sealed record DashboardOverviewResponse(
    int TotalLeads,
    int NewLeads,
    int InServiceLeads,
    int QualifiedLeads,
    int ConvertedLeads,
    int LostLeads,
    double AverageScore,
    double ConversionRate,
    IReadOnlyCollection<DashboardDimensionCount> ByTemperature,
    IReadOnlyCollection<DashboardDimensionCount> BySource);

public sealed record DashboardDimensionCount(string Name, int Count);
