namespace SWEN_DMS.DTOs.Messages;

public sealed class OcrRequestMessage
{
    public string Type { get; init; } = "ocr.request";
    public Guid DocumentId { get; init; }
    public string FileName { get; init; } = "";
    public DateTime UploadedAtUtc { get; init; }
}