using LiventaTransfer.Application.Common;
using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Application.DTOs.Notification;

public record NotificationListDto
{
    public long Id { get; init; }
    public long? JobId { get; init; }
    public string? JobNumber { get; init; }
    public RecipientType RecipientType { get; init; }
    public string RecipientTypeLabel { get; init; } = string.Empty;
    public Guid? RecipientUserId { get; init; }
    public string? RecipientUserName { get; init; }
    public NotificationChannel Channel { get; init; }
    public string ChannelLabel { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime? SentAt { get; init; }
    public bool IsDelivered { get; init; }
    public DateTime CreatedAt { get; init; }

    public static NotificationListDto FromEntity(Domain.Entities.Notification e) => new()
    {
        Id = e.Id,
        JobId = e.JobId,
        JobNumber = e.Job?.JobNumber,
        RecipientType = e.RecipientType,
        RecipientTypeLabel = EnumLabelHelper.GetLabel(e.RecipientType),
        RecipientUserId = e.RecipientUserId,
        RecipientUserName = e.RecipientUser != null ? $"{e.RecipientUser.FirstName} {e.RecipientUser.LastName}" : null,
        Channel = e.Channel,
        ChannelLabel = EnumLabelHelper.GetLabel(e.Channel),
        Message = e.Message,
        SentAt = e.SentAt,
        IsDelivered = e.IsDelivered,
        CreatedAt = e.CreatedAt
    };
}

public record CreateNotificationRequest
{
    public long? JobId { get; init; }
    public RecipientType RecipientType { get; init; }
    public string? RecipientPhone { get; init; }
    public Guid? RecipientUserId { get; init; }
    public NotificationChannel Channel { get; init; }
    public string Message { get; init; } = string.Empty;
}
