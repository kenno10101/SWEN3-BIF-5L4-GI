namespace SWEN_DMS.DTOs.Notes;

public class DocumentNoteDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}