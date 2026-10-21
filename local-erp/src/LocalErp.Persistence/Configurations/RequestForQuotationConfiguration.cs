using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class RequestForQuotationConfiguration : IEntityTypeConfiguration<RequestForQuotation>
{
    public void Configure(EntityTypeBuilder<RequestForQuotation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RequestForQuotations");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.PurchaseRequestId).IsRequired();
        builder.Property(item => item.Status).IsRequired();
        builder.Property(item => item.CreatedByUserId).IsRequired();
        builder.Property(item => item.CreatedAtUtc).IsRequired();
        builder.Property(item => item.Notes).HasMaxLength(RequestForQuotation.NotesMaxLength);
        builder.Property(item => item.CancellationReason).HasMaxLength(RequestForQuotation.CancellationReasonMaxLength);

        builder.HasIndex(item => item.PurchaseRequestId);
        builder.HasIndex(item => item.Status);
        builder.HasIndex(item => item.CreatedAtUtc);
        builder.HasIndex(item => item.CreatedByUserId);
        builder.HasIndex(item => item.SentByUserId);
        builder.HasIndex(item => item.ClosedByUserId);
        builder.HasIndex(item => item.CancelledByUserId);

        builder.HasOne<PurchaseRequest>()
            .WithMany()
            .HasForeignKey(item => item.PurchaseRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.SentByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.CancelledByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(item => item.Lines)
            .WithOne()
            .HasForeignKey(line => line.RequestForQuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(item => item.Suppliers)
            .WithOne()
            .HasForeignKey(supplier => supplier.RequestForQuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(item => item.SupplierQuotations)
            .WithOne()
            .HasForeignKey(quotation => quotation.RequestForQuotationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
