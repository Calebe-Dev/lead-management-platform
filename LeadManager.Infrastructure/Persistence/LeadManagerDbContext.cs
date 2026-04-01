using Microsoft.EntityFrameworkCore;

namespace LeadManager.Infrastructure.Persistence;

public sealed class LeadManagerDbContext : DbContext
{
    public LeadManagerDbContext(DbContextOptions<LeadManagerDbContext> options) : base(options)
    {
    }

    internal DbSet<LeadRecord> Leads => Set<LeadRecord>();
    internal DbSet<LeadHistoryRecord> LeadHistory => Set<LeadHistoryRecord>();
    internal DbSet<RoundRobinStateRecord> RoundRobinStates => Set<RoundRobinStateRecord>();
    internal DbSet<CampaignRecord> Campaigns => Set<CampaignRecord>();
    internal DbSet<UserRecord> Users => Set<UserRecord>();
    internal DbSet<AssignmentRecord> Assignments => Set<AssignmentRecord>();
    internal DbSet<RefreshTokenRecord> RefreshTokens => Set<RefreshTokenRecord>();
    internal DbSet<OutboxMessageRecord> OutboxMessages => Set<OutboxMessageRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeadManagerDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
