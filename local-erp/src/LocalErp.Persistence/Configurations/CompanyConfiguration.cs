using LocalErp.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Companies");
        builder.HasKey(company => company.Id);

        builder.Property(company => company.Id)
            .ValueGeneratedNever();

        builder.Property(company => company.Name)
            .HasMaxLength(Company.NameMaxLength)
            .IsRequired();

        builder.Property(company => company.IsActive)
            .IsRequired();
    }
}
