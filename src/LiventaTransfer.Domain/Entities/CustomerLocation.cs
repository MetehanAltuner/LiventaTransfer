namespace LiventaTransfer.Domain.Entities;

public class CustomerLocation : BaseEntity
{
    public long CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public long LocationId { get; set; }
    public Location Location { get; set; } = null!;
}
