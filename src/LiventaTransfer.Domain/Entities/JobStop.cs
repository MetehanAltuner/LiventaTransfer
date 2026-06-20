using System.ComponentModel.DataAnnotations.Schema;

namespace LiventaTransfer.Domain.Entities;

public class JobStop : BaseEntity
{
    public long JobId { get; set; }
    public Job Job { get; set; } = null!;

    public int Sequence { get; set; }

    public long CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>Bu durağa bağlı yolcular. Bir durakta birden fazla yolcu olabilir.</summary>
    public ICollection<JobStopPassenger> Passengers { get; set; } = new List<JobStopPassenger>();

    /// <summary>Yolcu sayısı, durağa eklenen yolcu adedinden türetilir.</summary>
    [NotMapped]
    public int PassengerCount => Passengers?.Count ?? 0;

    public long? PickupLocationId { get; set; }
    public Location? PickupLocation { get; set; }

    public long? DropoffLocationId { get; set; }
    public Location? DropoffLocation { get; set; }

    public string? PickupAddress { get; set; }
    public string? DropoffAddress { get; set; }

    public string? FlightCode { get; set; }
    public string? Notes { get; set; }

    public decimal? SalePrice { get; set; }

    public DateTime? PickedUpAt { get; set; }
    public DateTime? DroppedOffAt { get; set; }
}
