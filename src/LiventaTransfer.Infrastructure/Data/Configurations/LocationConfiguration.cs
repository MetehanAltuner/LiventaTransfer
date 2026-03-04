using LiventaTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiventaTransfer.Infrastructure.Data.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.ShortCode)
            .HasMaxLength(20);

        builder.Property(l => l.Address)
            .HasMaxLength(500);

        builder.Property(l => l.Latitude)
            .HasPrecision(9, 6);

        builder.Property(l => l.Longitude)
            .HasPrecision(9, 6);

        builder.Property(l => l.IsActive)
            .HasDefaultValue(true);
    }
}
