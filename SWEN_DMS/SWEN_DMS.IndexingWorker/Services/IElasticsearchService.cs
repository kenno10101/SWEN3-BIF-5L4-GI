using SWEN_DMS.IndexingWorker.Models;

namespace SWEN_DMS.IndexingWorker.Services;

public interface IElasticsearchService
{
    Task EnsureIndexExistsAsync();
    Task IndexDocumentAsync(DocumentIndexModel document);
    Task<SearchResult> SearchDocumentsAsync(string query, int from = 0, int size = 10);
    Task DeleteDocumentAsync(Guid documentId);
}