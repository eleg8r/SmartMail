using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMail.Domain.Entities;
using SmartMail.Domain.ValueObjects;

namespace SmartMail.Persistence.Configurations;

public class EmailConfiguration : IEntityTypeConfiguration<Email>
{
    public void Configure(EntityTypeBuilder<Email> builder)
    {
        builder.ToTable("Emails");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .HasConversion(
                v => v.Value,
                v => TenantId.Create(v))
            .IsRequired();

        builder.OwnsOne(e => e.From, from =>
        {
            from.Property(f => f.Address).HasColumnName("FromAddress").IsRequired().HasMaxLength(256);
            from.Property(f => f.DisplayName).HasColumnName("FromDisplayName").HasMaxLength(256);
        });

        builder.OwnsOne(e => e.To, to =>
        {
            to.Property(t => t.Address).HasColumnName("ToAddress").IsRequired().HasMaxLength(256);
            to.Property(t => t.DisplayName).HasColumnName("ToDisplayName").HasMaxLength(256);
        });

        builder.OwnsOne(e => e.Content, content =>
        {
            content.Property(c => c.Subject).HasColumnName("Subject").IsRequired().HasMaxLength(500);
            content.Property(c => c.HtmlBody).HasColumnName("HtmlBody").IsRequired();
            content.Property(c => c.TextBody).HasColumnName("TextBody");
        });

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.BounceType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.ProviderUsed)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.TrackingId)
            .HasMaxLength(100);

        builder.Property(e => e.BounceReason)
            .HasMaxLength(1000);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2000);

        // Store CC and BCC as JSON
        builder.Property(e => e.Cc)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v.Select(cc => new { cc.Address, cc.DisplayName }), (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<dynamic>>(v, (System.Text.Json.JsonSerializerOptions?)null)!
                    .Select(cc => EmailAddress.Create(cc.GetProperty("Address").GetString()!, cc.GetProperty("DisplayName").GetString()))
                    .ToList()
            )
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Bcc)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v.Select(bcc => new { bcc.Address, bcc.DisplayName }), (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<dynamic>>(v, (System.Text.Json.JsonSerializerOptions?)null)!
                    .Select(bcc => EmailAddress.Create(bcc.GetProperty("Address").GetString()!, bcc.GetProperty("DisplayName").GetString()))
                    .ToList()
            )
            .HasColumnType("nvarchar(max)");

        // Store Metadata as JSON
        builder.Property(e => e.Metadata)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!
            )
            .HasColumnType("nvarchar(max)");

        builder.HasMany(e => e.Attachments)
            .WithOne()
            .HasForeignKey(a => a.EmailId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.CampaignId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.TrackingId).IsUnique();
        builder.HasIndex(e => e.ScheduledAt);
        builder.HasIndex(e => e.CreatedAt);
    }
}
