using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Units;
using LocalErp.Persistence.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PurchaseOrderLines");
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Id).ValueGeneratedNever();
        builder.Property(line => line.PurchaseOrderId).IsRequired();
        builder.Property(line => line.LineNumber).IsRequired();
        builder.Property(line => line.RequestForQuotationAwardLineId).IsRequired();
        builder.Property(line => line.DescriptionSnapshot)
            .HasMaxLength(PurchaseOrderLine.DescriptionMaxLength)
            .IsRequired();
        builder.Property(line => line.Quantity)
            .HasPrecision(18, 4)
            .IsRequired();
        builder.Property(line => line.MeasurementUnitId).IsRequired();
        builder.Property(line => line.MeasurementUnitNameSnapshot)
            .HasMaxLength(PurchaseOrderLine.MeasurementUnitNameMaxLength)
            .IsRequired();
        builder.Property(line => line.UnitPrice)
            .HasConversion<ScaledDecimalToInt64Converter>()
            .HasColumnType("INTEGER")
            .IsRequired();

        builder.HasIndex(line => new { line.PurchaseOrderId, line.LineNumber }).IsUnique();
        builder.HasIndex(line => line.RequestForQuotationAwardLineId).IsUnique();
        builder.HasIndex(line => line.MeasurementUnitId);

        builder.HasOne<RequestForQuotationAwardLine>()
            .WithMany()
            .HasForeignKey(line => line.RequestForQuotationAwardLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MeasurementUnit>()
            .WithMany()
            .HasForeignKey(line => line.MeasurementUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
