using LiventaTransfer.Application.Common;
using LiventaTransfer.Domain.Entities;
using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Application.DTOs.Job;

public enum StopProgress
{
    Pending = 0,
    PickedUp = 1,
    DroppedOff = 2
}

public record TransferPassengerDto
{
    public long PassengerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
}

public record TransferStopDto
{
    public long Id { get; init; }
    public int Sequence { get; init; }
    public List<TransferPassengerDto> Passengers { get; init; } = [];
    /// <summary>Yolcu isimleri, virgülle birleştirilmiş (geriye dönük uyumluluk).</summary>
    public string? PassengerName { get; init; }
    public string? PassengerPhone { get; init; }
    public int PassengerCount { get; init; }
    public string? PickupLocationName { get; init; }
    public string? PickupAddress { get; init; }
    public decimal? PickupLatitude { get; init; }
    public decimal? PickupLongitude { get; init; }
    public string? DropoffLocationName { get; init; }
    public string? DropoffAddress { get; init; }
    public decimal? DropoffLatitude { get; init; }
    public decimal? DropoffLongitude { get; init; }
    public string? FlightCode { get; init; }
    public string? Notes { get; init; }
    public DateTime? PickedUpAt { get; init; }
    public DateTime? DroppedOffAt { get; init; }
    public StopProgress Progress { get; init; }

    public static TransferStopDto FromEntity(JobStop s)
    {
        var passengers = s.Passengers
            .Where(p => p.Passenger != null)
            .Select(p => new TransferPassengerDto
            {
                PassengerId = p.PassengerId,
                Name = p.Passenger!.FullName,
                Phone = p.Passenger.Phone
            })
            .ToList();

        return new()
        {
            Id = s.Id,
            Sequence = s.Sequence,
            Passengers = passengers,
            PassengerName = passengers.Count > 0
                ? string.Join(", ", passengers.Select(p => p.Name))
                : null,
            PassengerPhone = passengers.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Phone))?.Phone,
            PassengerCount = passengers.Count,
            PickupLocationName = s.PickupLocation?.Name,
            PickupAddress = s.PickupLocation?.Address ?? s.PickupAddress,
            PickupLatitude = s.PickupLocation?.Latitude,
            PickupLongitude = s.PickupLocation?.Longitude,
            DropoffLocationName = s.DropoffLocation?.Name,
            DropoffAddress = s.DropoffLocation?.Address ?? s.DropoffAddress,
            DropoffLatitude = s.DropoffLocation?.Latitude,
            DropoffLongitude = s.DropoffLocation?.Longitude,
            FlightCode = s.FlightCode,
            Notes = s.Notes,
            PickedUpAt = s.PickedUpAt,
            DroppedOffAt = s.DroppedOffAt,
            Progress = s.DroppedOffAt.HasValue
                ? StopProgress.DroppedOff
                : s.PickedUpAt.HasValue
                    ? StopProgress.PickedUp
                    : StopProgress.Pending
        };
    }
}

public record NavigationTargetDto
{
    public string Type { get; init; } = string.Empty; // "pickup" | "dropoff"
    public long StopId { get; init; }
    public string? LocationName { get; init; }
    public string? Address { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
}

public record TransferDetailDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public string JobNumber { get; init; } = string.Empty;
    public DateOnly JobDate { get; init; }
    public TimeOnly JobTime { get; init; }
    public JobType JobType { get; init; }
    public string JobTypeLabel { get; init; } = string.Empty;
    public JobStatus Status { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
    public DriverStage DriverStage { get; init; }
    public string DriverStageLabel { get; init; } = string.Empty;
    public DateTime? ContactedAt { get; init; }
    public DateTime? DepartedAt { get; init; }
    public string? DriverName { get; init; }
    public string? DriverNumber { get; init; }
    public string OperationNumber { get; init; } = OperationPhone;
    public string? VehiclePlate { get; init; }
    public string? FlightCode { get; init; }
    public string? Notes { get; init; }
    public List<TransferStopDto> Stops { get; init; } = [];
    public NavigationTargetDto? NextNavigation { get; init; }

    private const string OperationPhone = "+90 543 301 8440";

    public static TransferDetailDto FromEntity(Domain.Entities.Job e)
    {
        var stops = e.Stops.OrderBy(s => s.Sequence).Select(TransferStopDto.FromEntity).ToList();

        var nextStop = stops.FirstOrDefault(s => s.Progress != StopProgress.DroppedOff);
        NavigationTargetDto? nav = null;
        if (nextStop is not null)
        {
            var targetIsPickup = nextStop.Progress == StopProgress.Pending;
            nav = new NavigationTargetDto
            {
                Type = targetIsPickup ? "pickup" : "dropoff",
                StopId = nextStop.Id,
                LocationName = targetIsPickup ? nextStop.PickupLocationName : nextStop.DropoffLocationName,
                Address = targetIsPickup ? nextStop.PickupAddress : nextStop.DropoffAddress,
                Latitude = targetIsPickup ? nextStop.PickupLatitude : nextStop.DropoffLatitude,
                Longitude = targetIsPickup ? nextStop.PickupLongitude : nextStop.DropoffLongitude
            };
        }

        var stage = DriverStageHelper.Resolve(e);

        var commonFlight = stops
            .Select(s => s.FlightCode)
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Distinct()
            .ToList();

        return new TransferDetailDto
        {
            Id = e.Id,
            PublicId = e.PublicId,
            JobNumber = e.JobNumber,
            JobDate = e.JobDate,
            JobTime = e.JobTime,
            JobType = e.JobType,
            JobTypeLabel = EnumLabelHelper.GetLabel(e.JobType),
            Status = e.Status,
            StatusLabel = EnumLabelHelper.GetLabel(e.Status),
            DriverStage = stage,
            DriverStageLabel = EnumLabelHelper.GetLabel(stage),
            ContactedAt = e.ContactedAt,
            DepartedAt = e.DepartedAt,
            DriverName = e.Driver?.FullName,
            DriverNumber = e.Driver?.Phone,
            OperationNumber = OperationPhone,
            VehiclePlate = e.Vehicle?.Plate,
            FlightCode = commonFlight.Count == 1 ? commonFlight[0] : null,
            Notes = e.Notes,
            Stops = stops,
            NextNavigation = nav
        };
    }
}

public static class DriverStageHelper
{
    /// <summary>
    /// Job + JobStop timestamp'lerinden DriverStage'i türetir. Monoton: ulaşılan en yüksek aşamayı döner.
    /// </summary>
    public static DriverStage Resolve(Domain.Entities.Job e)
    {
        if (e.Stops.Any(s => s.DroppedOffAt.HasValue))
            return DriverStage.DroppedOff;
        if (e.Stops.Any(s => s.PickedUpAt.HasValue))
            return DriverStage.PickedUp;
        if (e.DepartedAt.HasValue)
            return DriverStage.Departed;
        if (e.ContactedAt.HasValue)
            return DriverStage.Contacted;
        return DriverStage.NotStarted;
    }
}
