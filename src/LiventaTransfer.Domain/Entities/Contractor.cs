using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Domain.Entities;

/// <summary>
/// Yüklenici — müşterilerin üstünde yer alan üst kurum/acente (örn. "Tatil Sepeti").
/// Hiyerarşi: Yüklenici (Contractor) → Müşteri (Customer) → Yolcu (Passenger).
/// İçerik olarak Customer ile birebir aynıdır.
/// </summary>
public class Contractor : BaseEntity
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

    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
