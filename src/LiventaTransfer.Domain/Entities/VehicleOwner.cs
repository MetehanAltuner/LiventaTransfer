namespace LiventaTransfer.Domain.Entities;

public class VehicleOwner : BaseEntity
{
    public string Name { get; set; } = null!;
    public bool IsOwnFleet { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<Driver> Drivers { get; set; } = new List<Driver>();
}
