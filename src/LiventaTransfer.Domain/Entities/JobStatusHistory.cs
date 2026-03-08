using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Domain.Entities;

public class JobStatusHistory : BaseEntity
{
    public long JobId { get; set; }
    public JobStatus? OldStatus { get; set; }
    public JobStatus NewStatus { get; set; }
    public Guid ChangedByUserId { get; set; }
    public string? ChangeReason { get; set; }
    public DateTime ChangedAt { get; set; }

    public Job Job { get; set; } = null!;
    public User ChangedByUser { get; set; } = null!;
}
