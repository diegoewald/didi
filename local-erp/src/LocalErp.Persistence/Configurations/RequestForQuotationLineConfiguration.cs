using LocalErp.Domain.Purchasing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class RequestForQuotationLineConfiguration : IEntityTypeConfiguration<RequestForQuotationLine>
{
    public void Configure(EntityTypeBuilder<RequestForQuotationLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RequestForQuotationLines");
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Id).ValueGeneratedNever();
        builder.Property(line => line.RequestForQuotationId).IsRequired();
        builder.Property(line => line.LineNumber).IsRequired();
        builder.Property(line => line.PurchaseRequestLineId).IsRequired();

        builder.HasIndex(line => new { line.RequestForQuotationId, line.LineNumber }).IsUnique();
        builder.HasIndex(line => new { line.RequestForQuotationId, line.PurchaseRequestLineId }).IsUnique();
        builder.HasIndex(line => line.PurchaseRequestLineId);

        builder.HasOne<PurchaseRequestLine>()
            .WithMany()
            .HasForeignKey(line => line.PurchaseRequestLineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
