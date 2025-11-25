using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
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
    private readonly IMinioClient _minioClient;
    private readonly string _bucket;
    
    public DocumentController(DocumentService service, ILogger<DocumentController> logger, IMessagePublisher publisher, IMinioClient minioClient, string bucket)
    {
        _service = service;
        _logger = logger;
        _publisher = publisher;            // rabbitmq
        _minioClient = minioClient;
        _bucket = bucket;
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
        
        // Generate object name (ID + original filename)
        var key = $"{Guid.NewGuid()}_{dto.File.FileName}";

        // var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "FileStore");
        // Directory.CreateDirectory(uploadDir);
        //
        // var filePath = Path.Combine(uploadDir, $"{Guid.NewGuid()}_{dto.File.FileName}");
        // await using (var stream = new FileStream(filePath, FileMode.Create))
        // {
        //     await dto.File.CopyToAsync(stream);
        // }
        
        await using (var stream = dto.File.OpenReadStream())
        {
            await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_bucket)
                .WithObject(key)
                .WithStreamData(stream)
                .WithObjectSize(dto.File.Length)
                .WithContentType(dto.File.ContentType)
            );
        }

        var created = await _service.AddDocumentAsync(dto, key);
        
        // send MQ-message (empty OCR worker gets it)
        var msg = new OcrRequestMessage
        {
            DocumentId    = created.Id,                              
            FileName      = created.FileName,
            PdfKey        = key,
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