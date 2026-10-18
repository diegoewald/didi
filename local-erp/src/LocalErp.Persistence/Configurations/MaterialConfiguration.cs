using LocalErp.Domain.Materials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Materials");
        builder.HasKey(material => material.Id);

        builder.Property(material => material.Id)
            .ValueGeneratedNever();

        builder.Property(material => material.Name)
            .HasMaxLength(Material.NameMaxLength)
            .IsRequired();

        builder.Property(material => material.IsActive)
            .IsRequired();
    }
}
