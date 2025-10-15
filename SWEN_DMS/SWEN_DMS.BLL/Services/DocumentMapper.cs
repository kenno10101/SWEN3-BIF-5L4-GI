using SWEN_DMS.Models;
using SWEN_DMS.DTOs;

namespace SWEN_DMS.BLL.Services;

public static class DocumentMapper
{
    public static DocumentDto ToDto(Document doc)
    {
        return new DocumentDto
        {
            Id = doc.Id,
            FileName = doc.FileName,
            Summary = doc.Summary,
            Tags = doc.Tags,
            UploadedAt = doc.UploadedAt
        };
    }

    public static Document ToEntity(DocumentCreateDto dto, string filePath)
    {
        return new Document
        {
            Id = Guid.NewGuid(),
            FileName = dto.File.FileName,
            FilePath = filePath,
            Tags = dto.Tags,
            UploadedAt = DateTime.UtcNow
        };
    }
}