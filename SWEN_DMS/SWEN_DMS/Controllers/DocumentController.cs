using Microsoft.AspNetCore.Mvc;
using SWEN_DMS.BLL.Services;
using SWEN_DMS.BLL.Interfaces;
using SWEN_DMS.DTOs;
using SWEN_DMS.DTOs.Messages;


namespace SWEN_DMS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly DocumentService _service;
    private readonly ILogger<DocumentController> _logger;
    private readonly IMessagePublisher _publisher;   // rabbitmq
    
    public DocumentController(DocumentService service, ILogger<DocumentController> logger, IMessagePublisher publisher)
    {
        _service = service;
        _logger = logger;
        _publisher = publisher;            // rabbitmq
    }
    
    // all documents
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetAll()
    {
        _logger.LogInformation("Get all Documents");
        var docs = await _service.GetAllDocumentsAsync();
        _logger.LogInformation("Returning {Count} Documents", docs.Count());
        return Ok(docs);
    }

    // single document
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        _logger.LogInformation("Get Document by ID with ID {Id}", id);
        var doc = await _service.GetDocumentAsync(id);
        if (doc == null)
        {
            _logger.LogInformation("Document Not Found with ID {Id}", id);
            return NotFound();
        }
        _logger.LogInformation("Returning Document with ID {Id}", id);
        return Ok(doc);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadDocument([FromForm] DocumentCreateDto dto)
    {
        _logger.LogInformation("Upload Document");
        if (dto.File == null || dto.File.Length == 0)
        {
            _logger.LogInformation("Upload Failed: Document file is empty");
            return BadRequest("No file uploaded.");
        }

        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "FileStore");
        Directory.CreateDirectory(uploadDir);

        var filePath = Path.Combine(uploadDir, $"{Guid.NewGuid()}_{dto.File.FileName}");
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await dto.File.CopyToAsync(stream);
        }

        var created = await _service.AddDocumentAsync(dto, filePath);
        
        // send MQ-message (empty OCR worker gets it)
        var msg = new OcrRequestMessage
        {
            DocumentId    = created.Id,                              
            FileName      = created.FileName,
            UploadedAtUtc = created.UploadedAt.ToUniversalTime()     
        };
        await _publisher.PublishAsync(msg);
        
        _logger.LogInformation("Document uploaded successfully: {Id}", created.Id);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }
    
    [HttpDelete("{id}")]
    public async Task Delete(Guid id)
    {
        _logger.LogInformation("About to delete document with ID {Id}", id);
        await _service.DeleteDocumentAsync(id);
    }
}