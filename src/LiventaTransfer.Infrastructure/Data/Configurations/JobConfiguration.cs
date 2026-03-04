using LiventaTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiventaTransfer.Infrastructure.Data.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs");

        builder.Property(j => j.JobNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(j => j.JobNumber)
            .IsUnique();

        builder.HasIndex(j => j.JobDate);
        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.CustomerId);
        builder.HasIndex(j => j.DriverId);

        builder.Property(j => j.PickupAddress)
            .HasMaxLength(500);

        builder.Property(j => j.DropoffAddress)
            .HasMaxLength(500);

        builder.Property(j => j.RouteDescription)
            .HasMaxLength(1000);

        builder.Property(j => j.FlightCode)
            .HasMaxLength(20);

        builder.Property(j => j.ExtraInfo)
            .HasMaxLength(2000);

        builder.Property(j => j.Notes)
            .HasMaxLength(2000);

        builder.Property(j => j.SourceEmail)
            .HasMaxLength(500);

        builder.Property(j => j.SalePrice)
            .HasPrecision(18, 2);

        builder.Property(j => j.PurchasePrice)
            .HasPrecision(18, 2);

        builder.Property(j => j.ExtraCost)
            .HasPrecision(18, 2);

        builder.Property(j => j.PassengerCount)
            .HasDefaultValue(1);

        builder.HasOne(j => j.Customer)
            .WithMany(c => c.Jobs)
            .HasForeignKey(j => j.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.Passenger)
            .WithMany()
            .HasForeignKey(j => j.PassengerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(j => j.PickupLocation)
            .WithMany()
            .HasForeignKey(j => j.PickupLocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(j => j.DropoffLocation)
            .WithMany()
            .HasForeignKey(j => j.DropoffLocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(j => j.VehicleOwner)
            .WithMany()
            .HasForeignKey(j => j.VehicleOwnerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(j => j.Vehicle)
            .WithMany()
            .HasForeignKey(j => j.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(j => j.Driver)
            .WithMany()
            .HasForeignKey(j => j.DriverId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(j => j.CreatedByUser)
            .WithMany()
            .HasForeignKey(j => j.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.AssignedByUser)
            .WithMany()
            .HasForeignKey(j => j.AssignedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
