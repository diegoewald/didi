using LocalErp.Domain.Units;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class MeasurementUnitConfiguration : IEntityTypeConfiguration<MeasurementUnit>
{
    public void Configure(EntityTypeBuilder<MeasurementUnit> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("MeasurementUnits");
        builder.HasKey(unit => unit.Id);

        builder.Property(unit => unit.Id)
            .ValueGeneratedNever();

        builder.Property(unit => unit.Name)
            .HasMaxLength(MeasurementUnit.NameMaxLength)
            .IsRequired();

        builder.Property(unit => unit.IsActive)
            .IsRequired();
    }
}
