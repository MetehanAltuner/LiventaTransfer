namespace LiventaTransfer.Domain.Entities;

public class InvoiceItem : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Guid JobId { get; set; }
    public string Description { get; set; } = null!;
    public decimal Amount { get; set; }

    public Invoice Invoice { get; set; } = null!;
    public Job Job { get; set; } = null!;
}
