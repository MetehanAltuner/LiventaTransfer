using LiventaTransfer.Application.Common;
using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Application.DTOs.Job;

public record JobStopDto
{
    public long Id { get; init; }
    public int Sequence { get; init; }
    public long CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public long? PassengerId { get; init; }
    public string? PassengerName { get; init; }
    public int PassengerCount { get; init; }
    public long? PickupLocationId { get; init; }
    public string? PickupLocationName { get; init; }
    public long? DropoffLocationId { get; init; }
    public string? DropoffLocationName { get; init; }
    public string? PickupAddress { get; init; }
    public string? DropoffAddress { get; init; }
    public string? FlightCode { get; init; }
    public string? Notes { get; init; }
    public decimal? SalePrice { get; init; }

    public static JobStopDto FromEntity(Domain.Entities.JobStop s) => new()
    {
        Id = s.Id,
        Sequence = s.Sequence,
        CustomerId = s.CustomerId,
        CustomerName = s.Customer?.Name ?? string.Empty,
        PassengerId = s.PassengerId,
        PassengerName = s.Passenger?.FullName,
        PassengerCount = s.PassengerCount,
        PickupLocationId = s.PickupLocationId,
        PickupLocationName = s.PickupLocation?.Name,
        DropoffLocationId = s.DropoffLocationId,
        DropoffLocationName = s.DropoffLocation?.Name,
        PickupAddress = s.PickupAddress,
        DropoffAddress = s.DropoffAddress,
        FlightCode = s.FlightCode,
        Notes = s.Notes,
        SalePrice = s.SalePrice
    };
}

public record JobStopRequest
{
    public long CustomerId { get; init; }
    public long? PassengerId { get; init; }
    public int PassengerCount { get; init; } = 1;
    public long? PickupLocationId { get; init; }
    public long? DropoffLocationId { get; init; }
    public string? PickupAddress { get; init; }
    public string? DropoffAddress { get; init; }
    public string? FlightCode { get; init; }
    public string? Notes { get; init; }
    public decimal? SalePrice { get; init; }
}

public record DriverActiveJobDto
{
    public string JobNumber { get; init; } = string.Empty;
    public Guid PublicId { get; init; }
}

public record JobListDto
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
    public int StopCount { get; init; }
    public int TotalPassengerCount { get; init; }
    public string CustomerNames { get; init; } = string.Empty;
    public string? DriverName { get; init; }
    public decimal? TotalSalePrice { get; init; }
    public long? MergedIntoJobId { get; init; }

    public static JobListDto FromEntity(Domain.Entities.Job e) => new()
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
        DriverStage = DriverStageHelper.Resolve(e),
        DriverStageLabel = EnumLabelHelper.GetLabel(DriverStageHelper.Resolve(e)),
        StopCount = e.Stops.Count,
        TotalPassengerCount = e.Stops.Sum(s => s.PassengerCount),
        CustomerNames = string.Join(", ", e.Stops
            .Where(s => s.Customer != null)
            .Select(s => s.Customer!.Name)
            .Distinct()),
        DriverName = e.Driver?.FullName,
        TotalSalePrice = e.Stops.Any(s => s.SalePrice.HasValue)
            ? e.Stops.Where(s => s.SalePrice.HasValue).Sum(s => s.SalePrice!.Value)
            : null,
        MergedIntoJobId = e.MergedIntoJobId
    };
}

public record JobDetailDto
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
    public string? RouteDescription { get; init; }
    public string? ExtraInfo { get; init; }
    public string? Notes { get; init; }
    public string? SourceEmail { get; init; }
    public long? VehicleOwnerId { get; init; }
    public string? VehicleOwnerName { get; init; }
    public long? VehicleId { get; init; }
    public string? VehiclePlate { get; init; }
    public long? DriverId { get; init; }
    public string? DriverName { get; init; }
    public decimal? PurchasePrice { get; init; }
    public decimal? ExtraCost { get; init; }
    public decimal? TotalSalePrice { get; init; }
    public string CreatedByUserName { get; init; } = string.Empty;
    public string? AssignedByUserName { get; init; }
    public long? MergedIntoJobId { get; init; }
    public string? MergedIntoJobNumber { get; init; }
    public List<long> MergedJobIds { get; init; } = [];
    public List<JobStopDto> Stops { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static JobDetailDto FromEntity(Domain.Entities.Job e) => new()
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
        RouteDescription = e.RouteDescription,
        ExtraInfo = e.ExtraInfo,
        Notes = e.Notes,
        SourceEmail = e.SourceEmail,
        VehicleOwnerId = e.VehicleOwnerId,
        VehicleOwnerName = e.VehicleOwner?.Name,
        VehicleId = e.VehicleId,
        VehiclePlate = e.Vehicle?.Plate,
        DriverId = e.DriverId,
        DriverName = e.Driver?.FullName,
        PurchasePrice = e.PurchasePrice,
        ExtraCost = e.ExtraCost,
        TotalSalePrice = e.Stops.Any(s => s.SalePrice.HasValue)
            ? e.Stops.Where(s => s.SalePrice.HasValue).Sum(s => s.SalePrice!.Value)
            : null,
        CreatedByUserName = e.CreatedByUser != null ? $"{e.CreatedByUser.FirstName} {e.CreatedByUser.LastName}" : string.Empty,
        AssignedByUserName = e.AssignedByUser != null ? $"{e.AssignedByUser.FirstName} {e.AssignedByUser.LastName}" : null,
        MergedIntoJobId = e.MergedIntoJobId,
        MergedIntoJobNumber = e.MergedIntoJob?.JobNumber,
        MergedJobIds = e.MergedJobs.Select(m => m.Id).ToList(),
        Stops = e.Stops
            .OrderBy(s => s.Sequence)
            .Select(JobStopDto.FromEntity)
            .ToList(),
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}

public record CreateJobRequest
{
    public DateOnly JobDate { get; init; }
    public TimeOnly JobTime { get; init; }
    public JobType JobType { get; init; }
    public string? RouteDescription { get; init; }
    public string? ExtraInfo { get; init; }
    public string? Notes { get; init; }
    public string? SourceEmail { get; init; }
    public long? VehicleOwnerId { get; init; }
    public long? VehicleId { get; init; }
    public long? DriverId { get; init; }
    public decimal? PurchasePrice { get; init; }
    public decimal? ExtraCost { get; init; }
    public List<JobStopRequest> Stops { get; init; } = [];
}

public record UpdateJobRequest
{
    public DateOnly JobDate { get; init; }
    public TimeOnly JobTime { get; init; }
    public JobType JobType { get; init; }
    public string? RouteDescription { get; init; }
    public string? ExtraInfo { get; init; }
    public string? Notes { get; init; }
    public string? SourceEmail { get; init; }
    public long? VehicleOwnerId { get; init; }
    public long? VehicleId { get; init; }
    public long? DriverId { get; init; }
    public decimal? PurchasePrice { get; init; }
    public decimal? ExtraCost { get; init; }
    public List<JobStopRequest> Stops { get; init; } = [];
}

public record UpdateJobStatusRequest
{
    public JobStatus NewStatus { get; init; }
    public string? ChangeReason { get; init; }
}

public record MergeJobsRequest
{
    public List<long> SourceJobIds { get; init; } = [];
}
