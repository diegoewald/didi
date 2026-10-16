using LocalErp.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(role => role.Id);
        builder.Property(role => role.Id).ValueGeneratedNever();
        builder.Property(role => role.Name).HasMaxLength(Role.NameMaxLength).IsRequired();
        builder.Property(role => role.NormalizedName).HasMaxLength(Role.NameMaxLength).IsRequired();
        builder.Property(role => role.IsActive).IsRequired();
        builder.HasIndex(role => role.NormalizedName).IsUnique();
    }
}
