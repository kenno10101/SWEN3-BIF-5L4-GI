namespace SWEN_DMS.Models;

public class AccessLogDaily
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }
    
    public DateOnly DayUtc { get; set; }

    public int AccessCount { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    
    public Document? Document { get; set; }
}