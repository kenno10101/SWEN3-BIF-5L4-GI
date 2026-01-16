using Microsoft.EntityFrameworkCore;
using SWEN_DMS.Models;

namespace SWEN_DMS.DAL.Repositories;

public class DocumentNoteRepository : IDocumentNoteRepository
{
    private readonly AppDbContext _ctx;

    public DocumentNoteRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task AddAsync(DocumentNote note)
    {
        _ctx.DocumentNotes.Add(note);
        await _ctx.SaveChangesAsync();
    }

    public async Task<IEnumerable<DocumentNote>> GetByDocumentIdAsync(Guid documentId)
    {
        return await _ctx.DocumentNotes
            .Where(n => n.DocumentId == documentId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<DocumentNote?> GetByIdAsync(Guid noteId)
    {
        return await _ctx.DocumentNotes.FirstOrDefaultAsync(n => n.Id == noteId);
    }

    public async Task DeleteAsync(Guid noteId)
    {
        var note = await _ctx.DocumentNotes.FirstOrDefaultAsync(n => n.Id == noteId);
        if (note == null) return;

        _ctx.DocumentNotes.Remove(note);
        await _ctx.SaveChangesAsync();
    }
}