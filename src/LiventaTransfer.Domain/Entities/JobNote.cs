namespace LiventaTransfer.Domain.Entities;

public class JobNote : BaseEntity
{
    public Guid JobId { get; set; }
    public string NoteText { get; set; } = null!;
    public Guid CreatedByUserId { get; set; }

    public Job Job { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
}
