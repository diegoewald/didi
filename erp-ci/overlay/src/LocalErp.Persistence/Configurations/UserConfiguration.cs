using LocalErp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Users");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .ValueGeneratedNever();

        builder.Property(user => user.Username)
            .HasMaxLength(User.UsernameMaxLength)
            .IsRequired();

        builder.Property(user => user.NormalizedUsername)
            .HasMaxLength(User.UsernameMaxLength)
            .IsRequired();

        builder.HasIndex(user => user.NormalizedUsername)
            .IsUnique();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(User.PasswordHashMaxLength)
            .IsRequired();

        builder.Property(user => user.IsActive)
            .IsRequired();
    }
}
