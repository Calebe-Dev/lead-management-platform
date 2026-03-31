using LeadManager.Domain.Leads;

namespace LeadManager.Application.Leads;

public sealed record UpdateLeadStatusCommand(LeadStatus Status);
