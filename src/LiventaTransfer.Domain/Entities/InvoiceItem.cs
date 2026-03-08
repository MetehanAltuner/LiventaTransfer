namespace LiventaTransfer.Domain.Entities;

public class InvoiceItem : BaseEntity
{
    public long InvoiceId { get; set; }
    public long JobId { get; set; }
    public string Description { get; set; } = null!;
    public decimal Amount { get; set; }

    public Invoice Invoice { get; set; } = null!;
    public Job Job { get; set; } = null!;
}
