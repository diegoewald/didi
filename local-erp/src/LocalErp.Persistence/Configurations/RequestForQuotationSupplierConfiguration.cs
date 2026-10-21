using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class RequestForQuotationSupplierConfiguration : IEntityTypeConfiguration<RequestForQuotationSupplier>
{
    public void Configure(EntityTypeBuilder<RequestForQuotationSupplier> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RequestForQuotationSuppliers");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.RequestForQuotationId).IsRequired();
        builder.Property(item => item.SupplierId).IsRequired();

        builder.HasIndex(item => new { item.RequestForQuotationId, item.SupplierId }).IsUnique();
        builder.HasIndex(item => item.SupplierId);

        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(item => item.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
