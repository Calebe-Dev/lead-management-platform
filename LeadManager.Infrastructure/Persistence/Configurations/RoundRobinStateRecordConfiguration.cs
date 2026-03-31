using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeadManager.Infrastructure.Persistence.Configurations;

internal sealed class RoundRobinStateRecordConfiguration : IEntityTypeConfiguration<RoundRobinStateRecord>
{
    public void Configure(EntityTypeBuilder<RoundRobinStateRecord> builder)
    {
        builder.ToTable("round_robin_state");
        builder.HasKey(state => state.Key);

        builder.Property(state => state.Key)
            .HasColumnName("key")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(state => state.LastIndex)
            .HasColumnName("last_index")
            .IsRequired();
    }
}
