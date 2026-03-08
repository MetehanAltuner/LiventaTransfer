using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Domain.Entities;

public class Notification : BaseEntity
{
    public long? JobId { get; set; }
    public RecipientType RecipientType { get; set; }
    public string? RecipientPhone { get; set; }
    public Guid? RecipientUserId { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Message { get; set; } = null!;
    public DateTime? SentAt { get; set; }
    public bool IsDelivered { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public Job? Job { get; set; }
    public User? RecipientUser { get; set; }
}
