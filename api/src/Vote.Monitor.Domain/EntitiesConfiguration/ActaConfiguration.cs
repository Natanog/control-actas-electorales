using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vote.Monitor.Domain.Entities.ActaAggregate;

namespace Vote.Monitor.Domain.EntitiesConfiguration;

internal class ActaConfiguration : IEntityTypeConfiguration<Acta>
{
    public void Configure(EntityTypeBuilder<Acta> builder)
    {
        builder.ToTable("Actas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ContestCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OriginalFileName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.StoredFileName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.FilePath).HasMaxLength(512).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Sha256Hash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ObserverNotes).HasMaxLength(2000);
        builder.Property(x => x.ReviewerNotes).HasMaxLength(4000);
        builder.Property(x => x.OcrProvider).HasMaxLength(100);
        builder.Property(x => x.OcrRawText).HasColumnType("text");
        builder.Property(x => x.OcrRawResponse).HasColumnType("jsonb");
        builder.Property(x => x.OcrValidationErrors).HasColumnType("text[]");
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.Sha256Hash);
        builder.HasIndex(x => new { x.ElectionRoundId, x.PollingStationId, x.ContestCode });
        builder.HasIndex(x => new { x.ElectionRoundId, x.Status });
        builder.HasIndex(x => new { x.ElectionRoundId, x.PollingStationId, x.ContestCode, x.IsPrimary })
            .IsUnique()
            .HasFilter("\"IsPrimary\" = TRUE");
        builder.HasOne(x => x.MonitoringObserver).WithMany().HasForeignKey(x => x.MonitoringObserverId);
        builder.HasMany(x => x.Results).WithOne().HasForeignKey(x => x.ActaId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Results).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal class ActaResultEntryConfiguration : IEntityTypeConfiguration<ActaResultEntry>
{
    public void Configure(EntityTypeBuilder<ActaResultEntry> builder)
    {
        builder.ToTable("ActaResultEntries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Label).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Confidence).HasPrecision(5, 4);
        builder.HasIndex(x => x.ActaId);
    }
}
