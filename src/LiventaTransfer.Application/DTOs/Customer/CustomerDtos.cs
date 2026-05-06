using LiventaTransfer.Application.Common;
using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Application.DTOs.Customer;

public record CustomerListDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public CustomerType CustomerType { get; init; }
    public string CustomerTypeLabel { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public bool IsActive { get; init; }

    public static CustomerListDto FromEntity(Domain.Entities.Customer entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        CustomerType = entity.CustomerType,
        CustomerTypeLabel = EnumLabelHelper.GetLabel(entity.CustomerType),
        Phone = entity.Phone,
        Email = entity.Email,
        IsActive = entity.IsActive
    };
}

public record CustomerDetailDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public CustomerType CustomerType { get; init; }
    public string CustomerTypeLabel { get; init; } = string.Empty;
    public string? TaxNumber { get; init; }
    public string? TaxOffice { get; init; }
    public string? TcKimlikNo { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static CustomerDetailDto FromEntity(Domain.Entities.Customer entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        CustomerType = entity.CustomerType,
        CustomerTypeLabel = EnumLabelHelper.GetLabel(entity.CustomerType),
        TaxNumber = entity.TaxNumber,
        TaxOffice = entity.TaxOffice,
        TcKimlikNo = entity.TcKimlikNo,
        Phone = entity.Phone,
        Email = entity.Email,
        Address = entity.Address,
        Notes = entity.Notes,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}

public record CreateCustomerRequest
{
    public string Name { get; init; } = string.Empty;
    public CustomerType CustomerType { get; init; }
    public string? TaxNumber { get; init; }
    public string? TaxOffice { get; init; }
    public string? TcKimlikNo { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Notes { get; init; }
}

public record SetCustomerLocationsRequest
{
    public List<long> LocationIds { get; init; } = [];
}

public record UpdateCustomerRequest
{
    public string Name { get; init; } = string.Empty;
    public CustomerType CustomerType { get; init; }
    public string? TaxNumber { get; init; }
    public string? TaxOffice { get; init; }
    public string? TcKimlikNo { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
}
