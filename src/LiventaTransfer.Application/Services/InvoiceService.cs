using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Invoice;
using LiventaTransfer.Application.Interfaces;
using LiventaTransfer.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class InvoiceService
{
    private readonly IAppDbContext _db;
    public InvoiceService(IAppDbContext db) => _db = db;

    public async Task<ApiResult<PagedResult<InvoiceListDto>>> GetPagedAsync(
        PagedQuery query, long? customerId, InvoiceStatus? status,
        DateOnly? dateFrom, DateOnly? dateTo, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _db.Invoices.AsNoTracking()
            .Include(i => i.Customer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(i => i.InvoiceNumber.ToLower().Contains(query.Search.ToLower())
                          || i.Customer.Name.ToLower().Contains(query.Search.ToLower()));

        if (customerId.HasValue)
            q = q.Where(i => i.CustomerId == customerId.Value);

        if (status.HasValue)
            q = q.Where(i => i.InvoiceStatus == status.Value);

        if (dateFrom.HasValue)
            q = q.Where(i => i.InvoiceDate >= dateFrom.Value);

        if (dateTo.HasValue)
            q = q.Where(i => i.InvoiceDate <= dateTo.Value);

        var total = await q.LongCountAsync(ct);

        q = (query.SortBy?.ToLower()) switch
        {
            "invoicenumber" => query.SortDesc ? q.OrderByDescending(i => i.InvoiceNumber) : q.OrderBy(i => i.InvoiceNumber),
            "invoicedate" => query.SortDesc ? q.OrderByDescending(i => i.InvoiceDate) : q.OrderBy(i => i.InvoiceDate),
            "customer" => query.SortDesc ? q.OrderByDescending(i => i.Customer.Name) : q.OrderBy(i => i.Customer.Name),
            _ => q.OrderByDescending(i => i.InvoiceDate)
        };

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => InvoiceListDto.FromEntity(i))
            .ToListAsync(ct);

        return ApiResult<PagedResult<InvoiceListDto>>.Ok(new PagedResult<InvoiceListDto>
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = total
        }, "Faturalar listelendi.");
    }

    public async Task<ApiResult<InvoiceDetailDto>> GetByIdAsync(long id, CancellationToken ct)
    {
        var entity = await _db.Invoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.InvoiceItems).ThenInclude(ii => ii.Job)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (entity is null)
            return ApiResult<InvoiceDetailDto>.Fail("Fatura bulunamadı.", statusCode: 404);

        return ApiResult<InvoiceDetailDto>.Ok(InvoiceDetailDto.FromEntity(entity), "Fatura bulundu.");
    }

    public async Task<ApiResult<InvoiceDetailDto>> CreateAsync(CreateInvoiceRequest request, CancellationToken ct)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct))
            return ApiResult<InvoiceDetailDto>.Fail("Müşteri bulunamadı.", statusCode: 400);

        var invoiceNumber = await GenerateInvoiceNumberAsync(ct);

        var entity = new Domain.Entities.Invoice
        {
            InvoiceNumber = invoiceNumber,
            CustomerId = request.CustomerId,
            InvoiceDate = request.InvoiceDate,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            TotalAmount = request.TotalAmount,
            TaxAmount = request.TaxAmount,
            GrandTotal = request.GrandTotal,
            InvoiceStatus = InvoiceStatus.Draft,
            Notes = request.Notes?.Trim()
        };

        _db.Invoices.Add(entity);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<ApiResult<InvoiceDetailDto>> UpdateAsync(long id, UpdateInvoiceRequest request, CancellationToken ct)
    {
        var entity = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (entity is null)
            return ApiResult<InvoiceDetailDto>.Fail("Fatura bulunamadı.", statusCode: 404);

        entity.InvoiceDate = request.InvoiceDate;
        entity.PeriodStart = request.PeriodStart;
        entity.PeriodEnd = request.PeriodEnd;
        entity.TotalAmount = request.TotalAmount;
        entity.TaxAmount = request.TaxAmount;
        entity.GrandTotal = request.GrandTotal;
        entity.InvoiceStatus = request.InvoiceStatus;
        entity.Notes = request.Notes?.Trim();

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<ApiResult<bool>> DeleteAsync(long id, CancellationToken ct)
    {
        var entity = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (entity is null)
            return ApiResult<bool>.Fail("Fatura bulunamadı.", statusCode: 404);

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "Fatura silindi.");
    }

    public async Task<ApiResult<InvoiceDetailDto>> AddItemAsync(long invoiceId, CreateInvoiceItemRequest request, CancellationToken ct)
    {
        if (!await _db.Invoices.AnyAsync(i => i.Id == invoiceId, ct))
            return ApiResult<InvoiceDetailDto>.Fail("Fatura bulunamadı.", statusCode: 404);

        if (!await _db.Jobs.AnyAsync(j => j.Id == request.JobId, ct))
            return ApiResult<InvoiceDetailDto>.Fail("İş bulunamadı.", statusCode: 400);

        var item = new Domain.Entities.InvoiceItem
        {
            InvoiceId = invoiceId,
            JobId = request.JobId,
            Description = request.Description.Trim(),
            Amount = request.Amount
        };

        _db.InvoiceItems.Add(item);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(invoiceId, ct);
    }

    public async Task<ApiResult<bool>> DeleteItemAsync(long itemId, CancellationToken ct)
    {
        var item = await _db.InvoiceItems.FirstOrDefaultAsync(i => i.Id == itemId, ct);
        if (item is null)
            return ApiResult<bool>.Fail("Fatura kalemi bulunamadı.", statusCode: 404);

        item.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "Fatura kalemi silindi.");
    }

    private async Task<string> GenerateInvoiceNumberAsync(CancellationToken ct)
    {
        var today = DateTime.UtcNow;
        var prefix = $"INV-{today:yyyyMM}";
        var count = await _db.Invoices.CountAsync(i => i.InvoiceNumber.StartsWith(prefix), ct);
        return $"{prefix}-{(count + 1):D4}";
    }
}
