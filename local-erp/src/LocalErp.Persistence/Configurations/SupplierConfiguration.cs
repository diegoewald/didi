using LocalErp.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Suppliers");
        builder.HasKey(supplier => supplier.Id);

        builder.Property(supplier => supplier.Id)
            .ValueGeneratedNever();

        builder.Property(supplier => supplier.Name)
            .HasMaxLength(Supplier.NameMaxLength)
            .IsRequired();

        builder.Property(supplier => supplier.IsActive)
            .IsRequired();
    }
}
