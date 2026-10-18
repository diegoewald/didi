using LocalErp.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class CompanyBranchConfiguration : IEntityTypeConfiguration<CompanyBranch>
{
    public void Configure(EntityTypeBuilder<CompanyBranch> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CompanyBranches");
        builder.HasKey(branch => branch.Id);

        builder.Property(branch => branch.Id)
            .ValueGeneratedNever();

        builder.Property(branch => branch.CompanyId)
            .IsRequired();

        builder.Property(branch => branch.Name)
            .HasMaxLength(CompanyBranch.NameMaxLength)
            .IsRequired();

        builder.Property(branch => branch.IsActive)
            .IsRequired();

        builder.HasIndex(branch => branch.CompanyId);

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(branch => branch.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
