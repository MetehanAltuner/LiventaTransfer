using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Customer;
using LiventaTransfer.Application.DTOs.Location;
using LiventaTransfer.Application.DTOs.Passenger;
using LiventaTransfer.Application.Interfaces;
using LiventaTransfer.Domain.Entities;
using LiventaTransfer.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class CustomerService
{
    private readonly IAppDbContext _db;
    public CustomerService(IAppDbContext db) => _db = db;

    public async Task<ApiResult<PagedResult<CustomerListDto>>> GetPagedAsync(PagedQuery query, CustomerType? customerType, long? locationId, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _db.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(c => c.Name.ToLower().Contains(query.Search.ToLower()));

        if (query.IsActive.HasValue)
            q = q.Where(c => c.IsActive == query.IsActive.Value);

        if (customerType.HasValue)
            q = q.Where(c => c.CustomerType == customerType.Value);

        if (locationId.HasValue)
            q = q.Where(c => c.CustomerLocations.Any(cl => cl.LocationId == locationId.Value));

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
            .Include(c => c.Passengers)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (entity is null)
            return ApiResult<CustomerDetailDto>.Fail("Müşteri bulunamadı.", statusCode: 404);

        return ApiResult<CustomerDetailDto>.Ok(CustomerDetailDto.FromEntity(entity), "Müşteri bulundu.");
    }

    public async Task<ApiResult<CustomerDetailDto>> CreateAsync(CreateCustomerRequest request, CancellationToken ct)
    {
        var entity = new Domain.Entities.Customer
        {
            Name = request.Name.Trim(),
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

        _db.Customers.Add(entity);
        await _db.SaveChangesAsync(ct);

        return ApiResult<CustomerDetailDto>.Ok(CustomerDetailDto.FromEntity(entity), "Müşteri oluşturuldu.", 201);
    }

    public async Task<ApiResult<CustomerDetailDto>> UpdateAsync(long id, UpdateCustomerRequest request, CancellationToken ct)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null)
            return ApiResult<CustomerDetailDto>.Fail("Müşteri bulunamadı.", statusCode: 404);

        entity.Name = request.Name.Trim();
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

    public async Task<ApiResult<List<LocationListDto>>> GetLocationsAsync(long customerId, CancellationToken ct)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == customerId, ct))
            return ApiResult<List<LocationListDto>>.Fail("Müşteri bulunamadı.", statusCode: 404);

        var locations = await _db.CustomerLocations
            .AsNoTracking()
            .Where(cl => cl.CustomerId == customerId)
            .Select(cl => cl.Location)
            .OrderBy(l => l.Name)
            .Select(l => LocationListDto.FromEntity(l))
            .ToListAsync(ct);

        return ApiResult<List<LocationListDto>>.Ok(locations, "Lokasyonlar listelendi.");
    }

    public async Task<ApiResult<List<LocationListDto>>> SetLocationsAsync(long customerId, SetCustomerLocationsRequest request, CancellationToken ct)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == customerId, ct))
            return ApiResult<List<LocationListDto>>.Fail("Müşteri bulunamadı.", statusCode: 404);

        var requestedIds = request.LocationIds.Distinct().ToList();

        if (requestedIds.Count > 0)
        {
            var existingLocationIds = await _db.Locations
                .Where(l => requestedIds.Contains(l.Id))
                .Select(l => l.Id)
                .ToListAsync(ct);

            var missing = requestedIds.Except(existingLocationIds).ToList();
            if (missing.Count > 0)
                return ApiResult<List<LocationListDto>>.Fail(
                    $"Lokasyon bulunamadı: {string.Join(", ", missing)}", statusCode: 404);
        }

        var current = await _db.CustomerLocations
            .Where(cl => cl.CustomerId == customerId)
            .ToListAsync(ct);

        var currentIds = current.Select(cl => cl.LocationId).ToHashSet();
        var requestedSet = requestedIds.ToHashSet();

        var toRemove = current.Where(cl => !requestedSet.Contains(cl.LocationId)).ToList();
        if (toRemove.Count > 0)
            _db.CustomerLocations.RemoveRange(toRemove);

        foreach (var locationId in requestedIds.Where(id => !currentIds.Contains(id)))
        {
            _db.CustomerLocations.Add(new CustomerLocation
            {
                CustomerId = customerId,
                LocationId = locationId
            });
        }

        await _db.SaveChangesAsync(ct);

        return await GetLocationsAsync(customerId, ct);
    }

    public async Task<ApiResult<List<PassengerListDto>>> GetPassengersAsync(long customerId, CancellationToken ct)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == customerId, ct))
            return ApiResult<List<PassengerListDto>>.Fail("Müşteri bulunamadı.", statusCode: 404);

        var passengers = await _db.Passengers
            .AsNoTracking()
            .Include(p => p.Customer)
            .Where(p => p.CustomerId == customerId)
            .Select(p => PassengerListDto.FromEntity(p))
            .ToListAsync(ct);

        return ApiResult<List<PassengerListDto>>.Ok(passengers, "Yolcular listelendi.");
    }
}
