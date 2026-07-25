namespace LiventaTransfer.Domain.Entities;

public class Passenger : BaseEntity
{
    public string FullName { get; set; } = null!;
    public string? NationalId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// VIP yolcu mu. True ise bu yolcuyu içeren iş tek durak ve tek yolcudan oluşmak zorundadır
    /// (başka yolcu/durak eklenemez) ve bu işler birleştirilemez.
    /// </summary>
    public bool IsVip { get; set; }

    public ICollection<PassengerLocation> PassengerLocations { get; set; } = new List<PassengerLocation>();
}
