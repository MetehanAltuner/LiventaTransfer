using LiventaTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiventaTransfer.Infrastructure.Data.Configurations;

public class PassengerLocationConfiguration : IEntityTypeConfiguration<PassengerLocation>
{
    public void Configure(EntityTypeBuilder<PassengerLocation> builder)
    {
        builder.ToTable("PassengerLocations");

        builder.HasIndex(pl => new { pl.PassengerId, pl.LocationId })
            .IsUnique();

        builder.HasOne(pl => pl.Passenger)
            .WithMany(p => p.PassengerLocations)
            .HasForeignKey(pl => pl.PassengerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pl => pl.Location)
            .WithMany(l => l.PassengerLocations)
            .HasForeignKey(pl => pl.LocationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
