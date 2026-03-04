using LiventaTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiventaTransfer.Infrastructure.Data.Configurations;

public class TripLogConfiguration : IEntityTypeConfiguration<TripLog>
{
    public void Configure(EntityTypeBuilder<TripLog> builder)
    {
        builder.ToTable("TripLogs");

        builder.Property(t => t.StartKm)
            .HasPrecision(10, 2);

        builder.Property(t => t.EndKm)
            .HasPrecision(10, 2);

        builder.Property(t => t.FlightStatus)
            .HasMaxLength(200);

        builder.Property(t => t.DriverNotes)
            .HasMaxLength(2000);

        builder.HasOne(t => t.Job)
            .WithMany(j => j.TripLogs)
            .HasForeignKey(t => t.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Driver)
            .WithMany()
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
