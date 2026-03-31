using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeadManager.Infrastructure.Persistence.Configurations;

internal sealed class LeadHistoryRecordConfiguration : IEntityTypeConfiguration<LeadHistoryRecord>
{
    public void Configure(EntityTypeBuilder<LeadHistoryRecord> builder)
    {
        builder.ToTable("lead_history");
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(entry => entry.LeadId)
            .HasColumnName("lead_id")
            .IsRequired();

        builder.HasIndex(entry => entry.LeadId)
            .HasDatabaseName("ix_lead_history_lead_id");

        builder.Property(entry => entry.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(entry => entry.FieldName)
            .HasColumnName("field_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(entry => entry.OldValue)
            .HasColumnName("old_value")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(entry => entry.NewValue)
            .HasColumnName("new_value")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(entry => entry.ChangedAtUtc)
            .HasColumnName("changed_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
