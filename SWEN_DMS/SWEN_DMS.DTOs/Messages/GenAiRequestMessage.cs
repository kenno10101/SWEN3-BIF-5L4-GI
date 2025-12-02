namespace SWEN_DMS.DTOs.Messages;

public sealed class GenAiRequestMessage
{
    public string Type { get; init; } = "genai.request";
    public Guid DocumentId { get; init; }
    public string Text { get; init; } = "";    // the OCR-extracted text
    public string? Model { get; init; }        // optional override
}