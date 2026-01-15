using SWEN_DMS.DTOs.Notes;
using SWEN_DMS.Models;

namespace SWEN_DMS.BLL.Services;

public static class DocumentNoteMapper
{
    public static DocumentNoteDto ToDto(DocumentNote note) => new()
    {
        Id = note.Id,
        DocumentId = note.DocumentId,
        Content = note.Content,
        CreatedAtUtc = note.CreatedAtUtc
    };
}