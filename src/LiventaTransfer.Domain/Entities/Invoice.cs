using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Domain.Entities;

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = null!;
    public long CustomerId { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public InvoiceStatus InvoiceStatus { get; set; }
    public string? Notes { get; set; }

    public Customer Customer { get; set; } = null!;
    public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
}
