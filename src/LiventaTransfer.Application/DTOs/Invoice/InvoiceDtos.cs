using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Application.DTOs.Invoice;

public record InvoiceListDto
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public DateOnly InvoiceDate { get; init; }
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public decimal GrandTotal { get; init; }
    public InvoiceStatus InvoiceStatus { get; init; }

    public static InvoiceListDto FromEntity(Domain.Entities.Invoice e) => new()
    {
        Id = e.Id,
        InvoiceNumber = e.InvoiceNumber,
        CustomerName = e.Customer?.Name ?? string.Empty,
        InvoiceDate = e.InvoiceDate,
        PeriodStart = e.PeriodStart,
        PeriodEnd = e.PeriodEnd,
        GrandTotal = e.GrandTotal,
        InvoiceStatus = e.InvoiceStatus
    };
}

public record InvoiceDetailDto
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public DateOnly InvoiceDate { get; init; }
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal GrandTotal { get; init; }
    public InvoiceStatus InvoiceStatus { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<InvoiceItemDto> Items { get; init; } = [];

    public record InvoiceItemDto
    {
        public Guid Id { get; init; }
        public Guid JobId { get; init; }
        public string JobNumber { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal Amount { get; init; }
    }

    public static InvoiceDetailDto FromEntity(Domain.Entities.Invoice e) => new()
    {
        Id = e.Id,
        InvoiceNumber = e.InvoiceNumber,
        CustomerId = e.CustomerId,
        CustomerName = e.Customer?.Name ?? string.Empty,
        InvoiceDate = e.InvoiceDate,
        PeriodStart = e.PeriodStart,
        PeriodEnd = e.PeriodEnd,
        TotalAmount = e.TotalAmount,
        TaxAmount = e.TaxAmount,
        GrandTotal = e.GrandTotal,
        InvoiceStatus = e.InvoiceStatus,
        Notes = e.Notes,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        Items = e.InvoiceItems
            .Where(i => !i.IsDeleted)
            .Select(i => new InvoiceItemDto
            {
                Id = i.Id,
                JobId = i.JobId,
                JobNumber = i.Job?.JobNumber ?? string.Empty,
                Description = i.Description,
                Amount = i.Amount
            }).ToList()
    };
}

public record CreateInvoiceRequest
{
    public Guid CustomerId { get; init; }
    public DateOnly InvoiceDate { get; init; }
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal GrandTotal { get; init; }
    public string? Notes { get; init; }
}

public record UpdateInvoiceRequest
{
    public DateOnly InvoiceDate { get; init; }
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal GrandTotal { get; init; }
    public InvoiceStatus InvoiceStatus { get; init; }
    public string? Notes { get; init; }
}

public record CreateInvoiceItemRequest
{
    public Guid JobId { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}
