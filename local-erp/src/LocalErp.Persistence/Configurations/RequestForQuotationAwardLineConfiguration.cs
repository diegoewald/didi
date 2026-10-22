using LocalErp.Domain.Purchasing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class RequestForQuotationAwardLineConfiguration : IEntityTypeConfiguration<RequestForQuotationAwardLine>
{
    public void Configure(EntityTypeBuilder<RequestForQuotationAwardLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RequestForQuotationAwardLines");
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Id).ValueGeneratedNever();
        builder.Property(line => line.RequestForQuotationAwardId).IsRequired();
        builder.Property(line => line.RequestForQuotationLineId).IsRequired();
        builder.Property(line => line.SupplierQuotationLineId).IsRequired();

        builder.HasIndex(line => new { line.RequestForQuotationAwardId, line.RequestForQuotationLineId }).IsUnique();
        builder.HasIndex(line => line.RequestForQuotationLineId);
        builder.HasIndex(line => line.SupplierQuotationLineId).IsUnique();

        builder.HasOne<RequestForQuotationLine>()
            .WithMany()
            .HasForeignKey(line => line.RequestForQuotationLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<SupplierQuotationLine>()
            .WithMany()
            .HasForeignKey(line => line.SupplierQuotationLineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
