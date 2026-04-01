namespace LeadManager.Application.Abstractions;

public interface IAssignmentRepository
{
    Task AddAsync(Guid leadId, string assignee, string reason, DateTime assignedAtUtc, CancellationToken cancellationToken = default);
}
