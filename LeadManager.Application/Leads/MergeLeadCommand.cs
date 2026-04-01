using LeadManager.Domain.Leads;

namespace LeadManager.Application.Leads;

public sealed record MergeLeadCommand(Guid SourceLeadId, LeadMergePrecedence Precedence);
