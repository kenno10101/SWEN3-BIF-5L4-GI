using Microsoft.AspNetCore.Mvc;
using SWEN_DMS.BLL.Interfaces;

namespace SWEN_DMS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Search query cannot be empty");
        }

        try
        {
            var from = (page - 1) * pageSize;
            var result = await _searchService.SearchDocumentsAsync(q, from, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents");
            return StatusCode(500, "Error searching documents");
        }
    }
}