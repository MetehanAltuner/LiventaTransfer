using LiventaTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiventaTransfer.Infrastructure.Data.Configurations;

public class JobNoteConfiguration : IEntityTypeConfiguration<JobNote>
{
    public void Configure(EntityTypeBuilder<JobNote> builder)
    {
        builder.ToTable("JobNotes");

        builder.Property(n => n.NoteText)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasOne(n => n.Job)
            .WithMany(j => j.JobNotes)
            .HasForeignKey(n => n.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.CreatedByUser)
            .WithMany()
            .HasForeignKey(n => n.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
