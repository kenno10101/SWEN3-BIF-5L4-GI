using SWEN_DMS.Models;

namespace SWEN_DMS.Models;

public class DocumentNote
{
    public Guid Id { get; set; }

    // Foreign Key to Document
    public Guid DocumentId { get; set; }

    // Note content
    public string Content { get; set; } = string.Empty;

    // Always store UTC
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Document? Document { get; set; }
}