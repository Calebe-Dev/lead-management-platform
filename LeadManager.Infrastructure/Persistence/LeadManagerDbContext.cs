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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeadManagerDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
