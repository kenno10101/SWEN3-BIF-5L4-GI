namespace SWEN_DMS.Models;

public class Document
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public string? ExtractedText { get; set; }
    public string? Summary { get; set; }
    public string? Tags { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}