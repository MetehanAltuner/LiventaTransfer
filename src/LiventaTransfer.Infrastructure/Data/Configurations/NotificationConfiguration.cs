using LiventaTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiventaTransfer.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.Property(n => n.RecipientPhone)
            .HasMaxLength(20);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasOne(n => n.Job)
            .WithMany()
            .HasForeignKey(n => n.JobId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(n => n.RecipientUser)
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
