namespace LiventaTransfer.Domain.Entities;

public class PassengerLocation : BaseEntity
{
    public long PassengerId { get; set; }
    public Passenger Passenger { get; set; } = null!;

    public long LocationId { get; set; }
    public Location Location { get; set; } = null!;
}
