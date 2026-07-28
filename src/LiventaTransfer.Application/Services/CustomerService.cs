using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Customer;
using LiventaTransfer.Application.Interfaces;
using LiventaTransfer.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class CustomerService
{
    private readonly IAppDbContext _db;
    public CustomerService(IAppDbContext db) => _db = db;

    public async Task<ApiResult<PagedResult<CustomerListDto>>> GetPagedAsync(PagedQuery query, CustomerType? customerType, long? contractorId, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _db.Customers.AsNoTracking().Include(c => c.Contractor).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(c => c.Name.ToLower().Contains(query.Search.ToLower()));

        if (query.IsActive.HasValue)
            q = q.Where(c => c.IsActive == query.IsActive.Value);

        if (customerType.HasValue)
            q = q.Where(c => c.CustomerType == customerType.Value);

        if (contractorId.HasValue)
            q = q.Where(c => c.ContractorId == contractorId.Value);

        var total = await q.LongCountAsync(ct);

        q = (query.SortBy?.ToLower()) switch
        {
            "name" => query.SortDesc ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name),
            _ => query.SortDesc ? q.OrderByDescending(c => c.CreatedAt) : q.OrderBy(c => c.Name)
        };

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => CustomerListDto.FromEntity(c))
            .ToListAsync(ct);

        return ApiResult<PagedResult<CustomerListDto>>.Ok(new PagedResult<CustomerListDto>
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = total
        }, "Müşteriler listelendi.");
    }

    public async Task<ApiResult<CustomerDetailDto>> GetByIdAsync(long id, CancellationToken ct)
    {
        var entity = await _db.Customers
            .AsNoTracking()
            .Include(c => c.Contractor)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (entity is null)
            return ApiResult<CustomerDetailDto>.Fail("Müşteri bulunamadı.", statusCode: 404);

        return ApiResult<CustomerDetailDto>.Ok(CustomerDetailDto.FromEntity(entity), "Müşteri bulundu.");
    }

    public async Task<ApiResult<CustomerDetailDto>> CreateAsync(CreateCustomerRequest request, CancellationToken ct)
    {
        if (request.ContractorId.HasValue && !await ContractorExistsAsync(request.ContractorId.Value, ct))
            return ApiResult<CustomerDetailDto>.Fail("Seçilen yüklenici bulunamadı.", statusCode: 400);

        var entity = new Domain.Entities.Customer
        {
            Name = NameFormatter.ToTitleCase(request.Name),
            CustomerType = request.CustomerType,
            TaxNumber = request.TaxNumber?.Trim(),
            TaxOffice = request.TaxOffice?.Trim(),
            TcKimlikNo = request.TcKimlikNo?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            Notes = request.Notes?.Trim(),
            ContractorId = request.ContractorId,
            IsActive = true
        };

        _db.Customers.Add(entity);
        await _db.SaveChangesAsync(ct);

        if (entity.ContractorId.HasValue)
            entity.Contractor = await _db.Contractors.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == entity.ContractorId.Value, ct);

        return ApiResult<CustomerDetailDto>.Ok(CustomerDetailDto.FromEntity(entity), "Müşteri oluşturuldu.", 201);
    }

    public async Task<ApiResult<CustomerDetailDto>> UpdateAsync(long id, UpdateCustomerRequest request, CancellationToken ct)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null)
            return ApiResult<CustomerDetailDto>.Fail("Müşteri bulunamadı.", statusCode: 404);

        if (request.ContractorId.HasValue && !await ContractorExistsAsync(request.ContractorId.Value, ct))
            return ApiResult<CustomerDetailDto>.Fail("Seçilen yüklenici bulunamadı.", statusCode: 400);

        entity.Name = NameFormatter.ToTitleCase(request.Name);
        entity.CustomerType = request.CustomerType;
        entity.TaxNumber = request.TaxNumber?.Trim();
        entity.TaxOffice = request.TaxOffice?.Trim();
        entity.TcKimlikNo = request.TcKimlikNo?.Trim();
        entity.Phone = request.Phone?.Trim();
        entity.Email = request.Email?.Trim();
        entity.Address = request.Address?.Trim();
        entity.Notes = request.Notes?.Trim();
        entity.ContractorId = request.ContractorId;
        if (request.IsActive.HasValue)
            entity.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync(ct);

        entity.Contractor = entity.ContractorId.HasValue
            ? await _db.Contractors.AsNoTracking().FirstOrDefaultAsync(c => c.Id == entity.ContractorId.Value, ct)
            : null;

        return ApiResult<CustomerDetailDto>.Ok(CustomerDetailDto.FromEntity(entity), "Müşteri güncellendi.");
    }

    public async Task<ApiResult<bool>> DeleteAsync(long id, CancellationToken ct)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null)
            return ApiResult<bool>.Fail("Müşteri bulunamadı.", statusCode: 404);

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "Müşteri silindi.");
    }

    private Task<bool> ContractorExistsAsync(long contractorId, CancellationToken ct) =>
        _db.Contractors.AsNoTracking().AnyAsync(c => c.Id == contractorId, ct);
}
