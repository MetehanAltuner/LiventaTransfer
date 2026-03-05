using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Application.DTOs.Customer;

public record CustomerListDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public CustomerType CustomerType { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public bool IsActive { get; init; }

    public static CustomerListDto FromEntity(Domain.Entities.Customer entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        CustomerType = entity.CustomerType,
        Phone = entity.Phone,
        Email = entity.Email,
        IsActive = entity.IsActive
    };
}

public record CustomerDetailDto
{
    public Guid Id { get; init; }
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
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<PassengerBrief> Passengers { get; init; } = [];

    public record PassengerBrief
    {
        public Guid Id { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string? Phone { get; init; }
        public bool IsActive { get; init; }
    }

    public static CustomerDetailDto FromEntity(Domain.Entities.Customer entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        CustomerType = entity.CustomerType,
        TaxNumber = entity.TaxNumber,
        TaxOffice = entity.TaxOffice,
        TcKimlikNo = entity.TcKimlikNo,
        Phone = entity.Phone,
        Email = entity.Email,
        Address = entity.Address,
        Notes = entity.Notes,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        Passengers = entity.Passengers
            .Where(p => !p.IsDeleted)
            .Select(p => new PassengerBrief
            {
                Id = p.Id,
                FullName = p.FullName,
                Phone = p.Phone,
                IsActive = p.IsActive
            }).ToList()
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
