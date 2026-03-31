using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LeadManager.Infrastructure.Persistence;

public sealed class LeadManagerDbContextFactory : IDesignTimeDbContextFactory<LeadManagerDbContext>
{
    public LeadManagerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LeadManagerDbContext>();
        const string connectionString = "Host=localhost;Port=5432;Database=lead_manager;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(connectionString);
        return new LeadManagerDbContext(optionsBuilder.Options);
    }
}
