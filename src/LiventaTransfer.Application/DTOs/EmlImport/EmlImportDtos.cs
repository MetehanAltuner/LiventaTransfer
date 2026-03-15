using LiventaTransfer.Application.DTOs.Job;

namespace LiventaTransfer.Application.DTOs.EmlImport;

public record EmlPassengerDto
{
    public long Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
}

public record EmlTransferDto
{
    public string Direction { get; init; } = string.Empty; // "Gidiş" / "Dönüş"
    public string? PickupAddress { get; init; }
    public string? DropoffAddress { get; init; }
    public string? FlightCode { get; init; }
    public DateOnly? FlightDate { get; init; }
    public TimeOnly? FlightDepartureTime { get; init; }
    public TimeOnly? FlightArrivalTime { get; init; }
    public string? FlightRoute { get; init; }
}

public record EmlParseResultDto
{
    public string SenderEmail { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public long CustomerId { get; init; }
    public EmlPassengerDto Passenger { get; init; } = new();
    public List<EmlTransferDto> Transfers { get; init; } = [];
    public string RawEmailBody { get; init; } = string.Empty;
}

public record ConfirmTransferItem
{
    public DateOnly JobDate { get; init; }
    public TimeOnly JobTime { get; init; }
    public string? PickupAddress { get; init; }
    public string? DropoffAddress { get; init; }
    public string? FlightCode { get; init; }
    public string? Notes { get; init; }
}

public record ConfirmEmlImportRequest
{
    public long CustomerId { get; init; }
    public long? PassengerId { get; init; }
    public Guid UserId { get; init; }
    public string? PassengerName { get; init; }
    public string? PassengerPhone { get; init; }
    public string? PassengerEmail { get; init; }
    public string? SourceEmail { get; init; }
    public List<ConfirmTransferItem> Transfers { get; init; } = [];
}

public record EmlImportResultDto
{
    public List<JobDetailDto> CreatedJobs { get; init; } = [];
}
