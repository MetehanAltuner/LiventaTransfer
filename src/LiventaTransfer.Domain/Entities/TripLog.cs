namespace LiventaTransfer.Domain.Entities;

public class TripLog : BaseEntity
{
    public long JobId { get; set; }
    public long DriverId { get; set; }
    public DateTime? PickupTime { get; set; }
    public DateTime? DropoffTime { get; set; }
    public decimal? StartKm { get; set; }
    public decimal? EndKm { get; set; }
    public int? WaitingMinutes { get; set; }
    public string? FlightStatus { get; set; }
    public string? DriverNotes { get; set; }

    public Job Job { get; set; } = null!;
    public Driver Driver { get; set; } = null!;
}
