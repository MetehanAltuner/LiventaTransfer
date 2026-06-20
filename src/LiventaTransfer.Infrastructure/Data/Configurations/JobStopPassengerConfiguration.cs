using LiventaTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiventaTransfer.Infrastructure.Data.Configurations;

public class JobStopPassengerConfiguration : IEntityTypeConfiguration<JobStopPassenger>
{
    public void Configure(EntityTypeBuilder<JobStopPassenger> builder)
    {
        builder.ToTable("JobStopPassengers");

        builder.HasIndex(x => new { x.JobStopId, x.PassengerId })
            .IsUnique();

        builder.HasOne(x => x.JobStop)
            .WithMany(s => s.Passengers)
            .HasForeignKey(x => x.JobStopId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Passenger)
            .WithMany()
            .HasForeignKey(x => x.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
