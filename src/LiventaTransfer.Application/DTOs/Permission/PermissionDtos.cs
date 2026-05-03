using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Application.DTOs.Permission;

public record PermissionDto
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public int SortOrder { get; init; }

    public static PermissionDto FromEntity(Domain.Entities.Permission p) => new()
    {
        Id = p.Id,
        Code = p.Code,
        Label = p.Label,
        Icon = p.Icon,
        SortOrder = p.SortOrder
    };
}

public record RolePermissionsDto
{
    public UserRole Role { get; init; }
    public string RoleLabel { get; init; } = string.Empty;
    public List<PermissionDto> Permissions { get; init; } = [];
}

public record UpdateRolePermissionsRequest
{
    public List<long> PermissionIds { get; init; } = [];
}
