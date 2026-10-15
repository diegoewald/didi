using LocalErp.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class RolePermissionGrantConfiguration : IEntityTypeConfiguration<RolePermissionGrant>
{
    public void Configure(EntityTypeBuilder<RolePermissionGrant> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(permission => new { permission.RoleId, permission.PermissionCode });
        builder.Property(permission => permission.PermissionCode)
            .HasMaxLength(PermissionCatalog.CodeMaxLength)
            .IsRequired();

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(permission => permission.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
