namespace LeadManager.Infrastructure.LeadDistribution;

public sealed class LeadDistributionOptions
{
    public List<LeadDistributionRuleOptions> Rules { get; init; } = [];
}

public sealed class LeadDistributionRuleOptions
{
    public string Name { get; init; } = string.Empty;
    public string? Region { get; init; }
    public string? LeadType { get; init; }
    public string? ProductInterest { get; init; }
    public int? MinScore { get; init; }
    public int? MaxScore { get; init; }
    public List<string> Assignees { get; init; } = [];
}
