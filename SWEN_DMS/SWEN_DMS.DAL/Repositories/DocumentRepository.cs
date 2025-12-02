using Microsoft.EntityFrameworkCore;
using SWEN_DMS.Models;

namespace SWEN_DMS.DAL.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _ctx;
    public DocumentRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<IEnumerable<Document>> GetAllAsync() =>
        await _ctx.Documents.ToListAsync();

    public async Task<Document?> GetByIdAsync(Guid id) =>
        await _ctx.Documents.FirstOrDefaultAsync(d => d.Id == id);

    public async Task AddAsync(Document document)
    {
        _ctx.Documents.Add(document);
        await _ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(Document document)
    {
        _ctx.Documents.Update(document);
        await _ctx.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var doc = await _ctx.Documents.FirstOrDefaultAsync(d => d.Id == id);
        if (doc != null)
        {
            _ctx.Documents.Remove(doc);
            await _ctx.SaveChangesAsync();
        }
    }

    public async Task UpdateExtractedTextAsync(Guid id, string text)
    {
        var doc = await _ctx.Documents.FirstOrDefaultAsync(d => d.Id == id)
                  ?? throw new KeyNotFoundException($"Document {id} not found");

        doc.ExtractedText = text;
        await _ctx.SaveChangesAsync();
    }
}
