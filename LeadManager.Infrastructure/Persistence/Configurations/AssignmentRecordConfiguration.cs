using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeadManager.Infrastructure.Persistence.Configurations;

internal sealed class AssignmentRecordConfiguration : IEntityTypeConfiguration<AssignmentRecord>
{
    public void Configure(EntityTypeBuilder<AssignmentRecord> builder)
    {
        builder.ToTable("assignments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.LeadId)
            .HasColumnName("lead_id")
            .IsRequired();

        builder.HasIndex(x => x.LeadId)
            .HasDatabaseName("ix_assignments_lead_id");

        builder.Property(x => x.Assignee)
            .HasColumnName("assignee")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.AssignedAtUtc)
            .HasColumnName("assigned_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
