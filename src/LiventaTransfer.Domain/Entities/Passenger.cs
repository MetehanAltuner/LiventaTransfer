namespace LiventaTransfer.Domain.Entities;

public class Passenger : BaseEntity
{
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
