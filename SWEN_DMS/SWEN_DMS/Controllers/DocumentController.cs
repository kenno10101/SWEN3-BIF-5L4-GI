using Microsoft.AspNetCore.Mvc;
using SWEN_DMS.BLL.Services;
using SWEN_DMS.DTOs;

namespace SWEN_DMS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly DocumentService _service;

    public DocumentController(DocumentService service)
    {
        _service = service;
    }

    // all documents
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetAll()
    {
        var docs = await _service.GetAllDocumentsAsync();
        return Ok(docs);
    }

    // single document
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var doc = await _service.GetDocumentAsync(id);
        return doc is null ? NotFound() : Ok(doc);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadDocument([FromForm] DocumentCreateDto dto)
    {
        if (dto.File == null || dto.File.Length == 0)
            return BadRequest("No file uploaded.");

        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "FileStore");
        Directory.CreateDirectory(uploadDir);

        var filePath = Path.Combine(uploadDir, $"{Guid.NewGuid()}_{dto.File.FileName}");
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await dto.File.CopyToAsync(stream);
        }

        var created = await _service.AddDocumentAsync(dto, filePath);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }
    
    [HttpDelete("{id}")]
    public async Task Delete(Guid id)
    {
        await _service.DeleteDocumentAsync(id);
    }
}