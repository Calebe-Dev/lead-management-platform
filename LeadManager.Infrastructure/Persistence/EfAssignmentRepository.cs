using LeadManager.Application.Abstractions;

namespace LeadManager.Infrastructure.Persistence;

public sealed class EfAssignmentRepository : IAssignmentRepository
{
    private readonly LeadManagerDbContext _dbContext;

    public EfAssignmentRepository(LeadManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Guid leadId, string assignee, string reason, DateTime assignedAtUtc, CancellationToken cancellationToken = default)
    {
        if (leadId == Guid.Empty)
        {
            throw new ArgumentException("Lead id is required.", nameof(leadId));
        }

        if (string.IsNullOrWhiteSpace(assignee))
        {
            throw new ArgumentException("Assignee is required.", nameof(assignee));
        }

        _dbContext.Assignments.Add(new AssignmentRecord
        {
            Id = Guid.NewGuid(),
            LeadId = leadId,
            Assignee = assignee.Trim(),
            Reason = string.IsNullOrWhiteSpace(reason) ? "manual" : reason.Trim(),
            AssignedAtUtc = assignedAtUtc == default ? DateTime.UtcNow : assignedAtUtc
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
