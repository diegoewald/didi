using LocalErp.Domain.Security;
using LocalErp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(assignment => new { assignment.UserId, assignment.RoleId });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(assignment => assignment.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(assignment => assignment.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
