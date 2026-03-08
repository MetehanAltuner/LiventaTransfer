using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Application.DTOs.Notification;

public record NotificationListDto
{
    public long Id { get; init; }
    public long? JobId { get; init; }
    public RecipientType RecipientType { get; init; }
    public NotificationChannel Channel { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime? SentAt { get; init; }
    public bool IsDelivered { get; init; }
    public DateTime CreatedAt { get; init; }

    public static NotificationListDto FromEntity(Domain.Entities.Notification e) => new()
    {
        Id = e.Id,
        JobId = e.JobId,
        RecipientType = e.RecipientType,
        Channel = e.Channel,
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
