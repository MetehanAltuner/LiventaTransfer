using LiventaTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiventaTransfer.Infrastructure.Data.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");

        builder.Property(v => v.Plate)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(v => v.Brand)
            .HasMaxLength(100);

        builder.Property(v => v.Model)
            .HasMaxLength(100);

        builder.Property(v => v.Capacity)
            .HasDefaultValue(4);

        builder.Property(v => v.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(v => v.VehicleOwner)
            .WithMany(o => o.Vehicles)
            .HasForeignKey(v => v.VehicleOwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
