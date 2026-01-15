using Microsoft.Extensions.Logging;
using SWEN_DMS.DAL.Repositories;
using SWEN_DMS.DTOs.Notes;
using SWEN_DMS.Models;

namespace SWEN_DMS.BLL.Services;

public class DocumentNoteService
{
    private readonly IDocumentNoteRepository _noteRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly ILogger<DocumentNoteService> _logger;

    public DocumentNoteService(
        IDocumentNoteRepository noteRepo,
        IDocumentRepository documentRepo,
        ILogger<DocumentNoteService> logger)
    {
        _noteRepo = noteRepo;
        _documentRepo = documentRepo;
        _logger = logger;
    }

    public async Task<DocumentNoteDto> AddNoteAsync(Guid documentId, DocumentNoteCreateDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var content = (dto.Content ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content is required.");

        // Ensure document exists (clean 404 instead of FK exception)
        var doc = await _documentRepo.GetByIdAsync(documentId);
        if (doc == null)
            throw new KeyNotFoundException($"Document {documentId} not found.");

        var note = new DocumentNote
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Content = content,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _noteRepo.AddAsync(note);

        _logger.LogInformation("Note {NoteId} added to document {DocumentId}", note.Id, documentId);
        return DocumentNoteMapper.ToDto(note);
    }

    public async Task<IEnumerable<DocumentNoteDto>> GetNotesAsync(Guid documentId)
    {
        // Optional: verify document exists (nice for 404)
        var doc = await _documentRepo.GetByIdAsync(documentId);
        if (doc == null)
            throw new KeyNotFoundException($"Document {documentId} not found.");

        var notes = await _noteRepo.GetByDocumentIdAsync(documentId);
        return notes.Select(DocumentNoteMapper.ToDto);
    }

    public async Task DeleteNoteAsync(Guid noteId)
    {
        var existing = await _noteRepo.GetByIdAsync(noteId);
        if (existing == null)
            throw new KeyNotFoundException($"Note {noteId} not found.");

        await _noteRepo.DeleteAsync(noteId);
        _logger.LogInformation("Note {NoteId} deleted", noteId);
    }
}
