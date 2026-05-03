namespace LiventaTransfer.Domain.Entities;

public class Permission : BaseEntity
{
    public string Code { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
