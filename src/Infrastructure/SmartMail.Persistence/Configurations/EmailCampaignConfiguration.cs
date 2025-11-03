using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMail.Domain.Entities;
using SmartMail.Domain.ValueObjects;

namespace SmartMail.Persistence.Configurations;

public class EmailCampaignConfiguration : IEntityTypeConfiguration<EmailCampaign>
{
    public void Configure(EntityTypeBuilder<EmailCampaign> builder)
    {
        builder.ToTable("EmailCampaigns");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId)
            .HasConversion(
                v => v.Value,
                v => TenantId.Create(v))
            .IsRequired();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.OwnsOne(c => c.FromAddress, from =>
        {
            from.Property(f => f.Address).HasColumnName("FromAddress").IsRequired().HasMaxLength(256);
            from.Property(f => f.DisplayName).HasColumnName("FromDisplayName").HasMaxLength(256);
        });

        builder.Property(c => c.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.HtmlTemplate)
            .IsRequired();

        builder.Property(c => c.TextTemplate);

        builder.Property(c => c.Type)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.TimeZone)
            .HasMaxLength(100);

        builder.Property(c => c.UnsubscribeUrl)
            .HasMaxLength(500);

        // Store RecipientListIds as JSON
        builder.Property(c => c.RecipientListIds)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!
            )
            .HasColumnType("nvarchar(max)");

        // Store Metadata as JSON
        builder.Property(c => c.Metadata)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!
            )
            .HasColumnType("nvarchar(max)");

        // Relationships
        builder.HasMany(c => c.Recipients)
            .WithOne()
            .HasForeignKey(r => r.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.BatchSchedules)
            .WithOne()
            .HasForeignKey(b => b.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Variants)
            .WithOne()
            .HasForeignKey(v => v.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.DripSteps)
            .WithOne()
            .HasForeignKey(d => d.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.ScheduledStartDate);
        builder.HasIndex(c => c.CreatedAt);
    }
}
