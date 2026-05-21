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

public record TransferStopDto
{
    public long Id { get; init; }
    public int Sequence { get; init; }
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
    public string? Message { get; init; }

    public static TransferStopDto FromEntity(JobStop s, Domain.Entities.Job job)
    {
        var progress = s.DroppedOffAt.HasValue
            ? StopProgress.DroppedOff
            : s.PickedUpAt.HasValue
                ? StopProgress.PickedUp
                : StopProgress.Pending;

        return new TransferStopDto
        {
            Id = s.Id,
            Sequence = s.Sequence,
            PassengerName = s.Passenger?.FullName,
            PassengerPhone = s.Passenger?.Phone,
            PassengerCount = s.PassengerCount,
            PickupLocationName = s.PickupLocation?.Name,
            PickupAddress = s.PickupAddress ?? s.PickupLocation?.Address,
            PickupLatitude = s.PickupLocation?.Latitude,
            PickupLongitude = s.PickupLocation?.Longitude,
            DropoffLocationName = s.DropoffLocation?.Name,
            DropoffAddress = s.DropoffAddress ?? s.DropoffLocation?.Address,
            DropoffLatitude = s.DropoffLocation?.Latitude,
            DropoffLongitude = s.DropoffLocation?.Longitude,
            FlightCode = s.FlightCode,
            Notes = s.Notes,
            PickedUpAt = s.PickedUpAt,
            DroppedOffAt = s.DroppedOffAt,
            Progress = progress,
            Message = BuildPassengerMessage(s, job)
        };
    }

    private const string OperationPhone = "+90 543 301 8440";

    private static string BuildPassengerMessage(JobStop s, Domain.Entities.Job job)
    {
        var passengerName = string.IsNullOrWhiteSpace(s.Passenger?.FullName) ? "Değerli Yolcumuz" : s.Passenger!.FullName;
        var dateTime = $"{job.JobDate:dd.MM.yyyy} {job.JobTime:HH:mm}";
        var pickup = s.PickupLocation?.Name ?? s.PickupAddress ?? s.PickupLocation?.Address ?? "-";
        var dropoff = s.DropoffLocation?.Name ?? s.DropoffAddress ?? s.DropoffLocation?.Address ?? "-";
        var driverName = job.Driver?.FullName ?? "-";
        var driverPhone = job.Driver?.Phone ?? "-";
        var plate = job.Vehicle?.Plate ?? "-";

        return
            $"Merhaba, {passengerName}. " +
            "Araç ve şoför bilgileriniz aşağıdaki gibidir: " +
            $"Tarih/Saat: {dateTime} | " +
            $"Alış: {pickup} | " +
            $"Bırakış: {dropoff} | " +
            $"Şoför: {driverName} | " +
            $"Şoför Telefon: {driverPhone} | " +
            $"Operasyon Hattı: {OperationPhone} | " +
            $"Araç Plakası: {plate}. " +
            "İyi yolculuklar dileriz.";
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
    public string? VehiclePlate { get; init; }
    public string? FlightCode { get; init; }
    public string? Notes { get; init; }
    public List<TransferStopDto> Stops { get; init; } = [];
    public NavigationTargetDto? NextNavigation { get; init; }

    public static TransferDetailDto FromEntity(Domain.Entities.Job e)
    {
        var stops = e.Stops.OrderBy(s => s.Sequence).Select(s => TransferStopDto.FromEntity(s, e)).ToList();

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
