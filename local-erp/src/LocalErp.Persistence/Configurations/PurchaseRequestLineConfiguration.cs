using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Units;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class PurchaseRequestLineConfiguration : IEntityTypeConfiguration<PurchaseRequestLine>
{
    public void Configure(EntityTypeBuilder<PurchaseRequestLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PurchaseRequestLines");
        builder.HasKey(line => line.Id);

        builder.Property(line => line.Id)
            .ValueGeneratedNever();

        builder.Property(line => line.PurchaseRequestId)
            .IsRequired();

        builder.Property(line => line.LineNumber)
            .IsRequired();

        builder.Property(line => line.Description)
            .HasMaxLength(PurchaseRequestLine.DescriptionMaxLength)
            .IsRequired();

        builder.Property(line => line.Quantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(line => line.MeasurementUnitId)
            .IsRequired();

        builder.HasIndex(line => new { line.PurchaseRequestId, line.LineNumber })
            .IsUnique();

        builder.HasOne<MeasurementUnit>()
            .WithMany()
            .HasForeignKey(line => line.MeasurementUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
