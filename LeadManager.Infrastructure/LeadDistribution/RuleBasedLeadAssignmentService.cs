using LeadManager.Application.Abstractions;
using LeadManager.Domain.Leads;
using Microsoft.Extensions.Options;

namespace LeadManager.Infrastructure.LeadDistribution;

public sealed class RuleBasedLeadAssignmentService : ILeadAssignmentService
{
    private readonly LeadDistributionOptions _options;
    private readonly IRoundRobinStateRepository _roundRobinStateRepository;

    public RuleBasedLeadAssignmentService(
        IOptions<LeadDistributionOptions> options,
        IRoundRobinStateRepository roundRobinStateRepository)
    {
        _options = options.Value;
        _roundRobinStateRepository = roundRobinStateRepository;
    }

    public async Task<string?> SelectAssigneeAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lead);

        foreach (var rule in _options.Rules)
        {
            if (!Matches(rule, lead) || rule.Assignees.Count == 0)
            {
                continue;
            }

            var index = await _roundRobinStateRepository.GetNextIndexAsync(
                $"{rule.Name}:{lead.Region}:{lead.Score}",
                rule.Assignees.Count,
                cancellationToken);

            return rule.Assignees[index];
        }

        return null;
    }

    private static bool Matches(LeadDistributionRuleOptions rule, Lead lead)
    {
        if (!string.IsNullOrWhiteSpace(rule.Region)
            && !string.Equals(rule.Region, lead.Region, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(rule.LeadType)
            && !string.Equals(rule.LeadType, lead.LeadType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(rule.ProductInterest)
            && !string.Equals(rule.ProductInterest, lead.ProductInterest, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (rule.MinScore.HasValue && lead.Score < rule.MinScore.Value)
        {
            return false;
        }

        if (rule.MaxScore.HasValue && lead.Score > rule.MaxScore.Value)
        {
            return false;
        }

        return true;
    }
}
