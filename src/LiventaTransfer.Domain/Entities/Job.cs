using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Domain.Entities;

public class Job : BaseEntity
{
    public string JobNumber { get; set; } = null!;
    public DateOnly JobDate { get; set; }
    public TimeOnly JobTime { get; set; }
    public JobType JobType { get; set; }
    public JobStatus Status { get; set; }
    public string? RouteDescription { get; set; }
    public string? ExtraInfo { get; set; }
    public string? Notes { get; set; }
    public string? SourceEmail { get; set; }
    public long? VehicleOwnerId { get; set; }
    public long? VehicleId { get; set; }
    public long? DriverId { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? ExtraCost { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? AssignedByUserId { get; set; }

    public long? MergedIntoJobId { get; set; }
    public Job? MergedIntoJob { get; set; }

    public VehicleOwner? VehicleOwner { get; set; }
    public Vehicle? Vehicle { get; set; }
    public Driver? Driver { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public User? AssignedByUser { get; set; }
    public ICollection<JobStop> Stops { get; set; } = new List<JobStop>();
    public ICollection<JobStatusHistory> StatusHistory { get; set; } = new List<JobStatusHistory>();
    public ICollection<TripLog> TripLogs { get; set; } = new List<TripLog>();
    public ICollection<JobNote> JobNotes { get; set; } = new List<JobNote>();
    public ICollection<Job> MergedJobs { get; set; } = new List<Job>();
}
