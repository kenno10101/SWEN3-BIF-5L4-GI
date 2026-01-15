namespace SWEN_DMS.IndexingWorker.Models;

public class DocumentIndexModel
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ExtractedText { get; set; }
    public string? Summary { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime UploadedAt { get; set; }
}

public class SearchResult
{
    public long TotalHits { get; set; }
    public List<SearchResultDocument> Documents { get; set; } = new();
}

public class SearchResultDocument
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ExtractedText { get; set; }
    public string? Summary { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime UploadedAt { get; set; }
    public double Score { get; set; }
    public IReadOnlyDictionary<string, IReadOnlyCollection<string>>? Highlights { get; set; }
}