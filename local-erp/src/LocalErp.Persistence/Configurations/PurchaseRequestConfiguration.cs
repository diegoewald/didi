using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class PurchaseRequestConfiguration : IEntityTypeConfiguration<PurchaseRequest>
{
    public void Configure(EntityTypeBuilder<PurchaseRequest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PurchaseRequests");
        builder.HasKey(request => request.Id);

        builder.Property(request => request.Id)
            .ValueGeneratedNever();

        builder.Property(request => request.Status)
            .IsRequired();

        builder.Property(request => request.RequesterUserId)
            .IsRequired();

        builder.Property(request => request.CreatedAtUtc)
            .IsRequired();

        builder.Property(request => request.Notes)
            .HasMaxLength(PurchaseRequest.NotesMaxLength);

        builder.Property(request => request.CancellationReason)
            .HasMaxLength(PurchaseRequest.CancellationReasonMaxLength);

        builder.HasIndex(request => request.Status);
        builder.HasIndex(request => request.CreatedAtUtc);
        builder.HasIndex(request => request.RequesterUserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(request => request.RequesterUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(request => request.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(request => request.CancelledByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(request => request.Lines)
            .WithOne()
            .HasForeignKey(line => line.PurchaseRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
