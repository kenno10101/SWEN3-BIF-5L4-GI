using SWEN_DMS.Models;

namespace SWEN_DMS.DAL.Repositories;

public interface IDocumentNoteRepository
{
    Task AddAsync(DocumentNote note);
    Task<IEnumerable<DocumentNote>> GetByDocumentIdAsync(Guid documentId);
    Task<DocumentNote?> GetByIdAsync(Guid noteId);
    Task DeleteAsync(Guid noteId);
}