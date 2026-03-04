using LiventaTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiventaTransfer.Infrastructure.Data.Configurations;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("Drivers");

        builder.Property(d => d.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(d => d.WhatsAppPhone)
            .HasMaxLength(20);

        builder.Property(d => d.LicenseNumber)
            .HasMaxLength(50);

        builder.Property(d => d.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(d => d.VehicleOwner)
            .WithMany(o => o.Drivers)
            .HasForeignKey(d => d.VehicleOwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.DefaultVehicle)
            .WithMany(v => v.Drivers)
            .HasForeignKey(d => d.DefaultVehicleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
