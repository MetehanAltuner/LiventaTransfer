using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; }
    public Guid BranchId { get; set; }
    public bool IsActive { get; set; } = true;

    public Branch Branch { get; set; } = null!;
}
