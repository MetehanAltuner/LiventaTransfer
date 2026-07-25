using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Contractor;
using LiventaTransfer.Application.Interfaces;
using LiventaTransfer.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class ContractorService
{
    private readonly IAppDbContext _db;
    public ContractorService(IAppDbContext db) => _db = db;

    public async Task<ApiResult<PagedResult<ContractorListDto>>> GetPagedAsync(PagedQuery query, CustomerType? customerType, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _db.Contractors.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(c => c.Name.ToLower().Contains(query.Search.ToLower()));

        if (query.IsActive.HasValue)
            q = q.Where(c => c.IsActive == query.IsActive.Value);

        if (customerType.HasValue)
            q = q.Where(c => c.CustomerType == customerType.Value);

        var total = await q.LongCountAsync(ct);

        q = (query.SortBy?.ToLower()) switch
        {
            "name" => query.SortDesc ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name),
            _ => query.SortDesc ? q.OrderByDescending(c => c.CreatedAt) : q.OrderBy(c => c.Name)
        };

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => ContractorListDto.FromEntity(c))
            .ToListAsync(ct);

        return ApiResult<PagedResult<ContractorListDto>>.Ok(new PagedResult<ContractorListDto>
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = total
        }, "Yükleniciler listelendi.");
    }

    public async Task<ApiResult<ContractorDetailDto>> GetByIdAsync(long id, CancellationToken ct)
    {
        var entity = await _db.Contractors
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (entity is null)
            return ApiResult<ContractorDetailDto>.Fail("Yüklenici bulunamadı.", statusCode: 404);

        return ApiResult<ContractorDetailDto>.Ok(ContractorDetailDto.FromEntity(entity), "Yüklenici bulundu.");
    }

    public async Task<ApiResult<ContractorDetailDto>> CreateAsync(CreateContractorRequest request, CancellationToken ct)
    {
        var entity = new Domain.Entities.Contractor
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
            IsActive = true
        };

        _db.Contractors.Add(entity);
        await _db.SaveChangesAsync(ct);

        return ApiResult<ContractorDetailDto>.Ok(ContractorDetailDto.FromEntity(entity), "Yüklenici oluşturuldu.", 201);
    }

    public async Task<ApiResult<ContractorDetailDto>> UpdateAsync(long id, UpdateContractorRequest request, CancellationToken ct)
    {
        var entity = await _db.Contractors.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null)
            return ApiResult<ContractorDetailDto>.Fail("Yüklenici bulunamadı.", statusCode: 404);

        entity.Name = NameFormatter.ToTitleCase(request.Name);
        entity.CustomerType = request.CustomerType;
        entity.TaxNumber = request.TaxNumber?.Trim();
        entity.TaxOffice = request.TaxOffice?.Trim();
        entity.TcKimlikNo = request.TcKimlikNo?.Trim();
        entity.Phone = request.Phone?.Trim();
        entity.Email = request.Email?.Trim();
        entity.Address = request.Address?.Trim();
        entity.Notes = request.Notes?.Trim();
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);

        return ApiResult<ContractorDetailDto>.Ok(ContractorDetailDto.FromEntity(entity), "Yüklenici güncellendi.");
    }

    public async Task<ApiResult<bool>> DeleteAsync(long id, CancellationToken ct)
    {
        var entity = await _db.Contractors.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null)
            return ApiResult<bool>.Fail("Yüklenici bulunamadı.", statusCode: 404);

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "Yüklenici silindi.");
    }
}
