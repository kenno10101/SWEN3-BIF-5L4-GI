using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SWEN_DMS.BLL.Interfaces;
using SWEN_DMS.DTOs;
using System.Text.Json.Serialization;

namespace SWEN_DMS.BLL.Services;

public class SearchService : ISearchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SearchService> _logger;
    private readonly string _elasticsearchUri;
    private readonly string _indexName;

    public SearchService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SearchService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _elasticsearchUri = configuration["Elasticsearch:Uri"] ?? "http://elasticsearch:9200";
        _indexName = configuration["Elasticsearch:IndexName"] ?? "documents";
    }

    public async Task<SearchResultDto> SearchDocumentsAsync(string query, int from = 0, int size = 10)
    {
        try
        {
            var searchRequest = new
            {
                from = from,
                size = size,
                query = new
                {
                    multi_match = new
                    {
                        query = query,
                        fields = new[] { "extractedText^2", "fileName^1.5", "summary", "tags" },
                        fuzziness = "AUTO"
                    }
                },
                highlight = new
                {
                    fields = new
                    {
                        extractedText = new { pre_tags = new[] { "<mark>" }, post_tags = new[] { "</mark>" } },
                        fileName = new { pre_tags = new[] { "<mark>" }, post_tags = new[] { "</mark>" } }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_elasticsearchUri}/{_indexName}/_search",
                searchRequest
            );

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ElasticsearchResponse>();

            if (result == null)
            {
                return new SearchResultDto();
            }

            return new SearchResultDto
            {
                TotalHits = result.Hits.Total.Value,
                Documents = result.Hits.Hits.Select(h => new SearchResultDocumentDto
                {
                    DocumentId = h.Source.DocumentId,
                    FileName = h.Source.FileName,
                    ExtractedText = h.Source.ExtractedText,
                    Summary = h.Source.Summary,
                    Tags = h.Source.Tags ?? new List<string>(),
                    UploadedAt = h.Source.UploadedAt,
                    Score = h.Score,
                    Highlights = h.Highlight?.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.ToList()
                    )
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents with query: {Query}", query);
            throw;
        }
    }

    // Internal classes for Elasticsearch response deserialization
    private class ElasticsearchResponse
    {
        [JsonPropertyName("hits")]
        public HitsContainer Hits { get; set; } = new();
    }

    private class HitsContainer
    {
        [JsonPropertyName("total")]
        public TotalHits Total { get; set; } = new();

        [JsonPropertyName("hits")]
        public List<Hit> Hits { get; set; } = new();
    }

    private class TotalHits
    {
        [JsonPropertyName("value")]
        public long Value { get; set; }
    }

    private class Hit
    {
        [JsonPropertyName("_score")]
        public double Score { get; set; }

        [JsonPropertyName("_source")]
        public DocumentSource Source { get; set; } = new();

        [JsonPropertyName("highlight")]
        public Dictionary<string, string[]>? Highlight { get; set; }
    }

    private class DocumentSource
    {
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? ExtractedText { get; set; }
        public string? Summary { get; set; }
        public List<string>? Tags { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}