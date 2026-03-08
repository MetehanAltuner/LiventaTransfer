using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Domain.Entities;

public class Vehicle : BaseEntity
{
    public string Plate { get; set; } = null!;
    public VehicleType VehicleType { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public int Capacity { get; set; } = 4;
    public long VehicleOwnerId { get; set; }
    public bool IsActive { get; set; } = true;

    public VehicleOwner VehicleOwner { get; set; } = null!;
    public ICollection<Driver> Drivers { get; set; } = new List<Driver>();
}
