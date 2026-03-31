using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeadManager.Infrastructure.Persistence.Configurations;

internal sealed class LeadRecordConfiguration : IEntityTypeConfiguration<LeadRecord>
{
    public void Configure(EntityTypeBuilder<LeadRecord> builder)
    {
        builder.ToTable("leads");
        builder.HasKey(lead => lead.Id);

        builder.Property(lead => lead.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(lead => lead.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(lead => lead.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        builder.HasIndex(lead => lead.Email)
            .HasDatabaseName("ix_leads_email")
            .IsUnique();

        builder.Property(lead => lead.Phone)
            .HasColumnName("phone")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(lead => lead.Company)
            .HasColumnName("company")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(lead => lead.JobTitle)
            .HasColumnName("job_title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(lead => lead.Source)
            .HasColumnName("source")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(lead => lead.Score)
            .HasColumnName("score")
            .IsRequired();

        builder.Property(lead => lead.Temperature)
            .HasColumnName("temperature")
            .IsRequired();

        builder.Property(lead => lead.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(lead => lead.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
