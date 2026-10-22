using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Suppliers;
using LocalErp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PurchaseOrders");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.SequenceNumber).IsRequired();
        builder.Property(item => item.RequestForQuotationAwardId).IsRequired();
        builder.Property(item => item.SupplierId).IsRequired();
        builder.Property(item => item.SupplierNameSnapshot)
            .HasMaxLength(PurchaseOrder.SupplierNameMaxLength)
            .IsRequired();
        builder.Property(item => item.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();
        builder.Property(item => item.Status).IsRequired();
        builder.Property(item => item.CreatedByUserId).IsRequired();
        builder.Property(item => item.CreatedAtUtc).IsRequired();
        builder.Property(item => item.Notes).HasMaxLength(PurchaseOrder.NotesMaxLength);
        builder.Property(item => item.CancellationReason)
            .HasMaxLength(PurchaseOrder.CancellationReasonMaxLength);

        builder.HasIndex(item => item.SequenceNumber).IsUnique();
        builder.HasIndex(item => new { item.RequestForQuotationAwardId, item.SupplierId }).IsUnique();
        builder.HasIndex(item => item.Status);
        builder.HasIndex(item => item.SupplierId);
        builder.HasIndex(item => item.CreatedAtUtc);
        builder.HasIndex(item => item.CreatedByUserId);
        builder.HasIndex(item => item.IssuedByUserId);
        builder.HasIndex(item => item.CancelledByUserId);

        builder.HasOne<RequestForQuotationAward>()
            .WithMany()
            .HasForeignKey(item => item.RequestForQuotationAwardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(item => item.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.IssuedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.CancelledByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(item => item.Lines)
            .WithOne()
            .HasForeignKey(line => line.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
