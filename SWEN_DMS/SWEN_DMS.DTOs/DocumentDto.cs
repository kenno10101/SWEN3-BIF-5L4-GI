namespace SWEN_DMS.DTOs;

public class DocumentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Tags { get; set; }
    public DateTime UploadedAt { get; set; }
}