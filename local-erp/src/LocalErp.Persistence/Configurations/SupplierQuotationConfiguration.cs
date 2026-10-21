using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Suppliers;
using LocalErp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class SupplierQuotationConfiguration : IEntityTypeConfiguration<SupplierQuotation>
{
    public void Configure(EntityTypeBuilder<SupplierQuotation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("SupplierQuotations");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.RequestForQuotationId).IsRequired();
        builder.Property(item => item.SupplierId).IsRequired();
        builder.Property(item => item.Status).IsRequired();
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(item => item.SupplierReference).HasMaxLength(SupplierQuotation.SupplierReferenceMaxLength);
        builder.Property(item => item.Notes).HasMaxLength(SupplierQuotation.NotesMaxLength);
        builder.Property(item => item.CreatedByUserId).IsRequired();
        builder.Property(item => item.CreatedAtUtc).IsRequired();

        builder.HasIndex(item => new { item.RequestForQuotationId, item.SupplierId }).IsUnique();
        builder.HasIndex(item => item.SupplierId);
        builder.HasIndex(item => item.CreatedByUserId);
        builder.HasIndex(item => item.FinalizedByUserId);

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
            .HasForeignKey(item => item.FinalizedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(item => item.Lines)
            .WithOne()
            .HasForeignKey(line => line.SupplierQuotationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
