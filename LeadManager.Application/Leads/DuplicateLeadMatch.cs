using LeadManager.Domain.Leads;

namespace LeadManager.Application.Leads;

public sealed record DuplicateLeadMatch(Lead Lead, IReadOnlyCollection<string> MatchedFields);
