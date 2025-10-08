using SWEN_DMS.Models;
using Microsoft.AspNetCore.Mvc;

namespace SWEN_DMS.Controllers;

[ApiController]
[Route("api/[controller]")]

public class DocumentController : ControllerBase
{
    private static readonly List<Document> _documents = new();

    [HttpGet("{id}")]
    public IActionResult Get(Guid id)
    {
        var doc = _documents.FirstOrDefault(d => d.Id == id);
        return doc is null ? NotFound() : Ok(doc);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "FileStore");
        Directory.CreateDirectory(uploadDir);

        var filePath = Path.Combine(uploadDir, $"{Guid.NewGuid()}_{file.FileName}");
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var extractedText = "[Simulated OCR Text]";
        var summary = "[Simulated AI Summary]";
        var tags = "sample,document";

        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FileName = file.FileName,
            FilePath = filePath,
            ExtractedText = extractedText,
            Summary = summary,
            Tags = tags,
            UploadedAt = DateTime.UtcNow
        };

        _documents.Add(doc);

        return CreatedAtAction(nameof(Get), new { id = doc.Id }, doc);
    }
    
    
}