using LiventaTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiventaTransfer.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(c => c.TaxNumber)
            .HasMaxLength(20);

        builder.HasIndex(c => c.TaxNumber)
            .IsUnique()
            .HasFilter("\"TaxNumber\" IS NOT NULL");

        builder.Property(c => c.TaxOffice)
            .HasMaxLength(200);

        builder.Property(c => c.TcKimlikNo)
            .HasMaxLength(11);

        builder.Property(c => c.Phone)
            .HasMaxLength(20);

        builder.Property(c => c.Email)
            .HasMaxLength(200);

        builder.Property(c => c.Address)
            .HasMaxLength(500);

        builder.Property(c => c.Notes)
            .HasMaxLength(2000);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);
    }
}
