namespace LiventaTransfer.Domain.Entities;

public abstract class BaseEntity : AuditableEntity
{
    public long Id { get; set; }
}
