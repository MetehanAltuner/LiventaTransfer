using LiventaTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiventaTransfer.Infrastructure.Data.Configurations;

public class CustomerLocationConfiguration : IEntityTypeConfiguration<CustomerLocation>
{
    public void Configure(EntityTypeBuilder<CustomerLocation> builder)
    {
        builder.ToTable("CustomerLocations");

        builder.HasIndex(cl => new { cl.CustomerId, cl.LocationId })
            .IsUnique();

        builder.HasOne(cl => cl.Customer)
            .WithMany(c => c.CustomerLocations)
            .HasForeignKey(cl => cl.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cl => cl.Location)
            .WithMany(l => l.CustomerLocations)
            .HasForeignKey(cl => cl.LocationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
