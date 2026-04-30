using LiventaTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiventaTransfer.Infrastructure.Data.Configurations;

public class JobStopConfiguration : IEntityTypeConfiguration<JobStop>
{
    public void Configure(EntityTypeBuilder<JobStop> builder)
    {
        builder.ToTable("JobStops");

        builder.HasIndex(s => new { s.JobId, s.Sequence });
        builder.HasIndex(s => s.CustomerId);
        builder.HasIndex(s => s.PickupLocationId);
        builder.HasIndex(s => s.DropoffLocationId);

        builder.Property(s => s.PickupAddress)
            .HasMaxLength(500);

        builder.Property(s => s.DropoffAddress)
            .HasMaxLength(500);

        builder.Property(s => s.FlightCode)
            .HasMaxLength(20);

        builder.Property(s => s.Notes)
            .HasMaxLength(1000);

        builder.Property(s => s.SalePrice)
            .HasPrecision(18, 2);

        builder.Property(s => s.PassengerCount)
            .HasDefaultValue(1);

        builder.HasOne(s => s.Job)
            .WithMany(j => j.Stops)
            .HasForeignKey(s => s.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Customer)
            .WithMany(c => c.JobStops)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Passenger)
            .WithMany()
            .HasForeignKey(s => s.PassengerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.PickupLocation)
            .WithMany()
            .HasForeignKey(s => s.PickupLocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.DropoffLocation)
            .WithMany()
            .HasForeignKey(s => s.DropoffLocationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
