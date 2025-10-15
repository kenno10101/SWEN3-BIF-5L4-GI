using SWEN_DMS.Models;
using SWEN_DMS.DTOs;
using SWEN_DMS.DAL.Repositories;

namespace SWEN_DMS.BLL.Services;

public class DocumentService
{
    private readonly IDocumentRepository _repository;

    public DocumentService(IDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<DocumentDto>> GetAllDocumentsAsync()
    {
        var documents = await _repository.GetAllAsync();
        return documents.Select(DocumentMapper.ToDto);
    }

    public async Task<DocumentDto?> GetDocumentAsync(Guid id)
    {
        var doc = await _repository.GetByIdAsync(id);
        return doc is null ? null : DocumentMapper.ToDto(doc);
    }

    public async Task<DocumentDto> AddDocumentAsync(DocumentCreateDto dto, string filePath)
    {
        var entity = DocumentMapper.ToEntity(dto, filePath);
        await _repository.AddAsync(entity);
        return DocumentMapper.ToDto(entity);
    }

    public async Task DeleteDocumentAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }
}