using SWEN_DMS.DTOs;

namespace SWEN_DMS.BLL.Interfaces;

public interface ISearchService
{
    Task<SearchResultDto> SearchDocumentsAsync(string query, int from = 0, int size = 10);
}