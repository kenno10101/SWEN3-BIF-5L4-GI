using SWEN_DMS.Models;
using SWEN_DMS.DTOs;
using SWEN_DMS.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace SWEN_DMS.BLL.Services;

public class DocumentService
{
    private readonly IDocumentRepository _repository;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(IDocumentRepository repository, ILogger<DocumentService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<DocumentDto>> GetAllDocumentsAsync()
    {
        _logger.LogInformation("GetAllDocumentsAsync from repository");
        var documents = await _repository.GetAllAsync();
        return documents.Select(DocumentMapper.ToDto);
    }

    public async Task<DocumentDto?> GetDocumentAsync(Guid id)
    {
        _logger.LogInformation("GetDocumentAsync from repository with ID {Id}", id);
        var doc = await _repository.GetByIdAsync(id);
        if (doc == null)
        {
            _logger.LogInformation("Document with ID {Id} not found", id);
            return null;
        }
        _logger.LogInformation("Returning Document with ID {Id}", id);
        return DocumentMapper.ToDto(doc);
    }

    public async Task<DocumentDto> AddDocumentAsync(DocumentCreateDto dto, string filePath)
    {
        _logger.LogInformation("AddDocumentAsync from repository");
        var entity = DocumentMapper.ToEntity(dto, filePath);
        await _repository.AddAsync(entity);
        return DocumentMapper.ToDto(entity);
    }

    public async Task DeleteDocumentAsync(Guid id)
    {
        _logger.LogInformation("Remove document with ID {Id} from repository", id);
        await _repository.DeleteAsync(id);
    }
    
    public async Task UpdateExtractedTextAsync(Guid id, string text)
    {
        _logger.LogInformation("Update ExtractedText for Document {Id}", id);
        await _repository.UpdateExtractedTextAsync(id, text);
    }

}