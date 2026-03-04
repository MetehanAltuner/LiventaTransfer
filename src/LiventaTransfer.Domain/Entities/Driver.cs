namespace LiventaTransfer.Domain.Entities;

public class Driver : BaseEntity
{
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? WhatsAppPhone { get; set; }
    public string? LicenseNumber { get; set; }
    public Guid VehicleOwnerId { get; set; }
    public Guid? DefaultVehicleId { get; set; }
    public bool IsActive { get; set; } = true;

    public VehicleOwner VehicleOwner { get; set; } = null!;
    public Vehicle? DefaultVehicle { get; set; }
}
