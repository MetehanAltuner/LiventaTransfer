using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Application.DTOs.Job;

public record JobListDto
{
    public Guid Id { get; init; }
    public string JobNumber { get; init; } = string.Empty;
    public DateOnly JobDate { get; init; }
    public TimeOnly JobTime { get; init; }
    public JobType JobType { get; init; }
    public JobStatus Status { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? PassengerName { get; init; }
    public string? DriverName { get; init; }
    public string? PickupLocationName { get; init; }
    public string? DropoffLocationName { get; init; }
    public int PassengerCount { get; init; }
    public decimal? SalePrice { get; init; }

    public static JobListDto FromEntity(Domain.Entities.Job e) => new()
    {
        Id = e.Id,
        JobNumber = e.JobNumber,
        JobDate = e.JobDate,
        JobTime = e.JobTime,
        JobType = e.JobType,
        Status = e.Status,
        CustomerName = e.Customer?.Name ?? string.Empty,
        PassengerName = e.Passenger?.FullName,
        DriverName = e.Driver?.FullName,
        PickupLocationName = e.PickupLocation?.Name,
        DropoffLocationName = e.DropoffLocation?.Name,
        PassengerCount = e.PassengerCount,
        SalePrice = e.SalePrice
    };
}

public record JobDetailDto
{
    public Guid Id { get; init; }
    public string JobNumber { get; init; } = string.Empty;
    public DateOnly JobDate { get; init; }
    public TimeOnly JobTime { get; init; }
    public JobType JobType { get; init; }
    public JobStatus Status { get; init; }
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public Guid? PassengerId { get; init; }
    public string? PassengerName { get; init; }
    public int PassengerCount { get; init; }
    public Guid? PickupLocationId { get; init; }
    public string? PickupLocationName { get; init; }
    public Guid? DropoffLocationId { get; init; }
    public string? DropoffLocationName { get; init; }
    public string? PickupAddress { get; init; }
    public string? DropoffAddress { get; init; }
    public string? RouteDescription { get; init; }
    public string? FlightCode { get; init; }
    public string? ExtraInfo { get; init; }
    public string? Notes { get; init; }
    public string? SourceEmail { get; init; }
    public Guid? VehicleOwnerId { get; init; }
    public string? VehicleOwnerName { get; init; }
    public Guid? VehicleId { get; init; }
    public string? VehiclePlate { get; init; }
    public Guid? DriverId { get; init; }
    public string? DriverName { get; init; }
    public decimal? SalePrice { get; init; }
    public decimal? PurchasePrice { get; init; }
    public decimal? ExtraCost { get; init; }
    public string CreatedByUserName { get; init; } = string.Empty;
    public string? AssignedByUserName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static JobDetailDto FromEntity(Domain.Entities.Job e) => new()
    {
        Id = e.Id,
        JobNumber = e.JobNumber,
        JobDate = e.JobDate,
        JobTime = e.JobTime,
        JobType = e.JobType,
        Status = e.Status,
        CustomerId = e.CustomerId,
        CustomerName = e.Customer?.Name ?? string.Empty,
        PassengerId = e.PassengerId,
        PassengerName = e.Passenger?.FullName,
        PassengerCount = e.PassengerCount,
        PickupLocationId = e.PickupLocationId,
        PickupLocationName = e.PickupLocation?.Name,
        DropoffLocationId = e.DropoffLocationId,
        DropoffLocationName = e.DropoffLocation?.Name,
        PickupAddress = e.PickupAddress,
        DropoffAddress = e.DropoffAddress,
        RouteDescription = e.RouteDescription,
        FlightCode = e.FlightCode,
        ExtraInfo = e.ExtraInfo,
        Notes = e.Notes,
        SourceEmail = e.SourceEmail,
        VehicleOwnerId = e.VehicleOwnerId,
        VehicleOwnerName = e.VehicleOwner?.Name,
        VehicleId = e.VehicleId,
        VehiclePlate = e.Vehicle?.Plate,
        DriverId = e.DriverId,
        DriverName = e.Driver?.FullName,
        SalePrice = e.SalePrice,
        PurchasePrice = e.PurchasePrice,
        ExtraCost = e.ExtraCost,
        CreatedByUserName = e.CreatedByUser != null ? $"{e.CreatedByUser.FirstName} {e.CreatedByUser.LastName}" : string.Empty,
        AssignedByUserName = e.AssignedByUser != null ? $"{e.AssignedByUser.FirstName} {e.AssignedByUser.LastName}" : null,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}

public record CreateJobRequest
{
    public DateOnly JobDate { get; init; }
    public TimeOnly JobTime { get; init; }
    public JobType JobType { get; init; }
    public Guid CustomerId { get; init; }
    public Guid? PassengerId { get; init; }
    public int PassengerCount { get; init; } = 1;
    public Guid? PickupLocationId { get; init; }
    public Guid? DropoffLocationId { get; init; }
    public string? PickupAddress { get; init; }
    public string? DropoffAddress { get; init; }
    public string? RouteDescription { get; init; }
    public string? FlightCode { get; init; }
    public string? ExtraInfo { get; init; }
    public string? Notes { get; init; }
    public string? SourceEmail { get; init; }
    public Guid? VehicleOwnerId { get; init; }
    public Guid? VehicleId { get; init; }
    public Guid? DriverId { get; init; }
    public decimal? SalePrice { get; init; }
    public decimal? PurchasePrice { get; init; }
    public decimal? ExtraCost { get; init; }
}

public record UpdateJobRequest
{
    public DateOnly JobDate { get; init; }
    public TimeOnly JobTime { get; init; }
    public JobType JobType { get; init; }
    public Guid CustomerId { get; init; }
    public Guid? PassengerId { get; init; }
    public int PassengerCount { get; init; } = 1;
    public Guid? PickupLocationId { get; init; }
    public Guid? DropoffLocationId { get; init; }
    public string? PickupAddress { get; init; }
    public string? DropoffAddress { get; init; }
    public string? RouteDescription { get; init; }
    public string? FlightCode { get; init; }
    public string? ExtraInfo { get; init; }
    public string? Notes { get; init; }
    public string? SourceEmail { get; init; }
    public Guid? VehicleOwnerId { get; init; }
    public Guid? VehicleId { get; init; }
    public Guid? DriverId { get; init; }
    public decimal? SalePrice { get; init; }
    public decimal? PurchasePrice { get; init; }
    public decimal? ExtraCost { get; init; }
}

public record UpdateJobStatusRequest
{
    public JobStatus NewStatus { get; init; }
    public string? ChangeReason { get; init; }
}
