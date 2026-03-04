using LiventaTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiventaTransfer.Infrastructure.Data.Configurations;

public class VehicleOwnerConfiguration : IEntityTypeConfiguration<VehicleOwner>
{
    public void Configure(EntityTypeBuilder<VehicleOwner> builder)
    {
        builder.ToTable("VehicleOwners");

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(v => v.ContactPerson)
            .HasMaxLength(200);

        builder.Property(v => v.Phone)
            .HasMaxLength(20);

        builder.Property(v => v.Email)
            .HasMaxLength(200);

        builder.Property(v => v.Notes)
            .HasMaxLength(1000);

        builder.Property(v => v.IsActive)
            .HasDefaultValue(true);
    }
}
