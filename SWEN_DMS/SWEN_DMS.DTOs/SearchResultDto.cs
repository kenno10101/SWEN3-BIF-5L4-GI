namespace SWEN_DMS.DTOs;

public class SearchResultDto
{
    public long TotalHits { get; set; }
    public List<SearchResultDocumentDto> Documents { get; set; } = new();
}

public class SearchResultDocumentDto
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ExtractedText { get; set; }
    public string? Summary { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime UploadedAt { get; set; }
    public double Score { get; set; }
    public Dictionary<string, List<string>>? Highlights { get; set; }
}