using LocalErp.Domain.Security;
using LocalErp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class SecurityAuditEventConfiguration : IEntityTypeConfiguration<SecurityAuditEvent>
{
    public void Configure(EntityTypeBuilder<SecurityAuditEvent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("SecurityAuditEvents");
        builder.HasKey(auditEvent => auditEvent.Id);
        builder.Property(auditEvent => auditEvent.Id).ValueGeneratedNever();
        builder.Property(auditEvent => auditEvent.OccurredAtUtc).IsRequired();
        builder.Property(auditEvent => auditEvent.EventCode)
            .HasMaxLength(SecurityAuditEvent.EventCodeMaxLength)
            .IsRequired();
        builder.Property(auditEvent => auditEvent.DetailCode)
            .HasMaxLength(SecurityAuditEvent.DetailCodeMaxLength);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(auditEvent => auditEvent.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(auditEvent => auditEvent.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(auditEvent => auditEvent.RoleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(auditEvent => auditEvent.OccurredAtUtc);
        builder.HasIndex(auditEvent => new { auditEvent.UserId, auditEvent.OccurredAtUtc });
        builder.HasIndex(auditEvent => new { auditEvent.ActorUserId, auditEvent.OccurredAtUtc });
        builder.HasIndex(auditEvent => new { auditEvent.RoleId, auditEvent.OccurredAtUtc });
        builder.HasIndex(auditEvent => new { auditEvent.EventCode, auditEvent.OccurredAtUtc });
    }
}
