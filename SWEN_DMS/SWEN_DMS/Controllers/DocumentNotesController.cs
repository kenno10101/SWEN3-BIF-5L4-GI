using Microsoft.AspNetCore.Mvc;
using SWEN_DMS.BLL.Services;
using SWEN_DMS.DTOs.Notes;

namespace SWEN_DMS.Controllers;

[ApiController]
[Route("api")]
public class DocumentNotesController : ControllerBase
{
    private readonly DocumentNoteService _service;
    private readonly ILogger<DocumentNotesController> _logger;

    public DocumentNotesController(DocumentNoteService service, ILogger<DocumentNotesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("documents/{documentId:guid}/notes")]
    public async Task<IActionResult> AddNote(Guid documentId, [FromBody] DocumentNoteCreateDto dto)
    {
        try
        {
            var created = await _service.AddNoteAsync(documentId, dto);
            return Created($"/api/documents/{documentId}/notes", created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding note to document {DocumentId}", documentId);
            return StatusCode(500, "Error adding note");
        }
    }

    [HttpGet("documents/{documentId:guid}/notes")]
    public async Task<IActionResult> GetNotes(Guid documentId)
    {
        try
        {
            var notes = await _service.GetNotesAsync(documentId);
            return Ok(notes);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching notes for document {DocumentId}", documentId);
            return StatusCode(500, "Error fetching notes");
        }
    }

    [HttpDelete("notes/{noteId:guid}")]
    public async Task<IActionResult> DeleteNote(Guid noteId)
    {
        try
        {
            await _service.DeleteNoteAsync(noteId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting note {NoteId}", noteId);
            return StatusCode(500, "Error deleting note");
        }
    }
}
