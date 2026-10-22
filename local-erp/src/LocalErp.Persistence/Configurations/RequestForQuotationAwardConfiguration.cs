using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalErp.Persistence.Configurations;

internal sealed class RequestForQuotationAwardConfiguration : IEntityTypeConfiguration<RequestForQuotationAward>
{
    public void Configure(EntityTypeBuilder<RequestForQuotationAward> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RequestForQuotationAwards");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.RequestForQuotationId).IsRequired();
        builder.Property(item => item.Status).IsRequired();
        builder.Property(item => item.CreatedByUserId).IsRequired();
        builder.Property(item => item.CreatedAtUtc).IsRequired();
        builder.Property(item => item.Notes).HasMaxLength(RequestForQuotationAward.NotesMaxLength);

        builder.HasIndex(item => item.RequestForQuotationId).IsUnique();
        builder.HasIndex(item => item.Status);
        builder.HasIndex(item => item.CreatedAtUtc);
        builder.HasIndex(item => item.CreatedByUserId);
        builder.HasIndex(item => item.FinalizedByUserId);

        builder.HasOne<RequestForQuotation>()
            .WithMany()
            .HasForeignKey(item => item.RequestForQuotationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.FinalizedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(item => item.Lines)
            .WithOne()
            .HasForeignKey(line => line.RequestForQuotationAwardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
