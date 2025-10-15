namespace SWEN_DMS.DTOs;

using Microsoft.AspNetCore.Http;

public class DocumentCreateDto
{
    public IFormFile File { get; set; } = default!;
    public string? Tags { get; set; }
}