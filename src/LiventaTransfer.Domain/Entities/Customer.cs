using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Domain.Entities;

public class Customer : BaseEntity
{
    public string Name { get; set; } = null!;
    public CustomerType CustomerType { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? TcKimlikNo { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Müşterinin bağlı olduğu yüklenici (opsiyonel). Hiyerarşi: Yüklenici → Müşteri → Yolcu.</summary>
    public long? ContractorId { get; set; }
    public Contractor? Contractor { get; set; }

    public ICollection<JobStop> JobStops { get; set; } = new List<JobStop>();
}
