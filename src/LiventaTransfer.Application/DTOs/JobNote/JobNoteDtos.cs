namespace LiventaTransfer.Application.DTOs.JobNote;

public record JobNoteDto
{
    public long Id { get; init; }
    public long JobId { get; init; }
    public string NoteText { get; init; } = string.Empty;
    public string CreatedByUserName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }

    public static JobNoteDto FromEntity(Domain.Entities.JobNote e) => new()
    {
        Id = e.Id,
        JobId = e.JobId,
        NoteText = e.NoteText,
        CreatedByUserName = e.CreatedByUser != null ? $"{e.CreatedByUser.FirstName} {e.CreatedByUser.LastName}" : string.Empty,
        CreatedAt = e.CreatedAt
    };
}

public record CreateJobNoteRequest
{
    public string NoteText { get; init; } = string.Empty;
}

public record UpdateJobNoteRequest
{
    public string NoteText { get; init; } = string.Empty;
}
