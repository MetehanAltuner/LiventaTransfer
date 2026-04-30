using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Domain.Entities;

public class Location : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? ShortCode { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public LocationType LocationType { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CustomerLocation> CustomerLocations { get; set; } = new List<CustomerLocation>();
}
