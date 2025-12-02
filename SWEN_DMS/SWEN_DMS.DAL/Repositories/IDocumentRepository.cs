using SWEN_DMS.Models;

namespace SWEN_DMS.DAL.Repositories;

public interface IDocumentRepository
{
    Task<IEnumerable<Document>> GetAllAsync();
    Task<Document?> GetByIdAsync(Guid id);
    Task AddAsync(Document document);
    Task UpdateAsync(Document document);
    Task DeleteAsync(Guid id);
    Task UpdateExtractedTextAsync(Guid id, string text);
    Task UpdateSummaryAsync(Guid id, string summary);
}