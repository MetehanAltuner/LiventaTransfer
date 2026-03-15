using LiventaTransfer.Application.Common;
using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Application.DTOs.JobStatusHistory;

public record JobStatusHistoryDto
{
    public long Id { get; init; }
    public JobStatus? OldStatus { get; init; }
    public string? OldStatusLabel { get; init; }
    public JobStatus NewStatus { get; init; }
    public string NewStatusLabel { get; init; } = string.Empty;
    public string ChangedByUserName { get; init; } = string.Empty;
    public string? ChangeReason { get; init; }
    public DateTime ChangedAt { get; init; }

    public static JobStatusHistoryDto FromEntity(Domain.Entities.JobStatusHistory e) => new()
    {
        Id = e.Id,
        OldStatus = e.OldStatus,
        OldStatusLabel = e.OldStatus.HasValue ? EnumLabelHelper.GetLabel(e.OldStatus.Value) : null,
        NewStatus = e.NewStatus,
        NewStatusLabel = EnumLabelHelper.GetLabel(e.NewStatus),
        ChangedByUserName = e.ChangedByUser != null ? $"{e.ChangedByUser.FirstName} {e.ChangedByUser.LastName}" : string.Empty,
        ChangeReason = e.ChangeReason,
        ChangedAt = e.ChangedAt
    };
}
