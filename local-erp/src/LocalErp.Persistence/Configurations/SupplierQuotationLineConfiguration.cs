using LocalErp.Domain.Purchasing;
using LocalErp.Persistence.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class SupplierQuotationLineConfiguration : IEntityTypeConfiguration<SupplierQuotationLine>
{
    public void Configure(EntityTypeBuilder<SupplierQuotationLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("SupplierQuotationLines");
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Id).ValueGeneratedNever();
        builder.Property(line => line.SupplierQuotationId).IsRequired();
        builder.Property(line => line.RequestForQuotationLineId).IsRequired();
        builder.Property(line => line.UnitPrice)
            .HasConversion<ScaledDecimalToInt64Converter>()
            .HasColumnType("INTEGER")
            .IsRequired();
        builder.Property(line => line.Notes).HasMaxLength(SupplierQuotationLine.NotesMaxLength);

        builder.HasIndex(line => new { line.SupplierQuotationId, line.RequestForQuotationLineId }).IsUnique();
        builder.HasIndex(line => line.RequestForQuotationLineId);

        builder.HasOne<RequestForQuotationLine>()
            .WithMany()
            .HasForeignKey(line => line.RequestForQuotationLineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
