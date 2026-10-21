using LocalErp.Domain.CostCenters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class CostCenterConfiguration : IEntityTypeConfiguration<CostCenter>
{
    public void Configure(EntityTypeBuilder<CostCenter> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CostCenters");
        builder.HasKey(costCenter => costCenter.Id);

        builder.Property(costCenter => costCenter.Id)
            .ValueGeneratedNever();

        builder.Property(costCenter => costCenter.Name)
            .HasMaxLength(CostCenter.NameMaxLength)
            .IsRequired();

        builder.Property(costCenter => costCenter.IsActive)
            .IsRequired();
    }
}
