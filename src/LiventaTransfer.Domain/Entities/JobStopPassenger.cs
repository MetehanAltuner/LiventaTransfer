namespace LiventaTransfer.Domain.Entities;

public class JobStopPassenger : BaseEntity
{
    public long JobStopId { get; set; }
    public JobStop JobStop { get; set; } = null!;

    public long PassengerId { get; set; }
    public Passenger Passenger { get; set; } = null!;

    /// <summary>Bu yolcuya transfer bilgisi gönderildiği an. Null ise bilgi henüz verilmemiştir. ContactedAt'tan bağımsızdır.</summary>
    public DateTime? InfoSentAt { get; set; }
}
