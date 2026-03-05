namespace LiventaTransfer.Application.DTOs.TripLog;

public record TripLogDto
{
    public Guid Id { get; init; }
    public Guid JobId { get; init; }
    public Guid DriverId { get; init; }
    public string DriverName { get; init; } = string.Empty;
    public DateTime? PickupTime { get; init; }
    public DateTime? DropoffTime { get; init; }
    public decimal? StartKm { get; init; }
    public decimal? EndKm { get; init; }
    public int? WaitingMinutes { get; init; }
    public string? FlightStatus { get; init; }
    public string? DriverNotes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static TripLogDto FromEntity(Domain.Entities.TripLog e) => new()
    {
        Id = e.Id,
        JobId = e.JobId,
        DriverId = e.DriverId,
        DriverName = e.Driver != null ? e.Driver.FullName : string.Empty,
        PickupTime = e.PickupTime,
        DropoffTime = e.DropoffTime,
        StartKm = e.StartKm,
        EndKm = e.EndKm,
        WaitingMinutes = e.WaitingMinutes,
        FlightStatus = e.FlightStatus,
        DriverNotes = e.DriverNotes,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}

public record CreateTripLogRequest
{
    public Guid DriverId { get; init; }
    public DateTime? PickupTime { get; init; }
    public DateTime? DropoffTime { get; init; }
    public decimal? StartKm { get; init; }
    public decimal? EndKm { get; init; }
    public int? WaitingMinutes { get; init; }
    public string? FlightStatus { get; init; }
    public string? DriverNotes { get; init; }
}

public record UpdateTripLogRequest
{
    public Guid DriverId { get; init; }
    public DateTime? PickupTime { get; init; }
    public DateTime? DropoffTime { get; init; }
    public decimal? StartKm { get; init; }
    public decimal? EndKm { get; init; }
    public int? WaitingMinutes { get; init; }
    public string? FlightStatus { get; init; }
    public string? DriverNotes { get; init; }
}
