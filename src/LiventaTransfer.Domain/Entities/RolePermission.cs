using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Domain.Entities;

public class RolePermission : BaseEntity
{
    public UserRole Role { get; set; }

    public long PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
