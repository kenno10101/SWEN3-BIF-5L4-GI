using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using SWEN_DMS.IndexingWorker.Models;

namespace SWEN_DMS.IndexingWorker.Services;

public class ElasticsearchService : IElasticsearchService
{
    private readonly IElasticClient _client;
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly string _indexName;

    public ElasticsearchService(
        IElasticClient client,
        IConfiguration configuration,
        ILogger<ElasticsearchService> logger)
    {
        _client = client;
        _logger = logger;
        _indexName = configuration["Elasticsearch:IndexName"] ?? "documents";
    }

    public async Task EnsureIndexExistsAsync()
    {
        var existsResponse = await _client.Indices.ExistsAsync(_indexName);
        
        if (!existsResponse.Exists)
        {
            _logger.LogInformation("Creating index {IndexName}", _indexName);
            
            var createResponse = await _client.Indices.CreateAsync(_indexName, c => c
                .Map<DocumentIndexModel>(m => m
                    .AutoMap()
                    .Properties(p => p
                        .Text(t => t
                            .Name(n => n.ExtractedText)
                            .Analyzer("standard"))
                        .Text(t => t
                            .Name(n => n.FileName)
                            .Analyzer("standard"))
                        .Keyword(k => k
                            .Name(n => n.Tags))
                    )
                )
            );

            if (!createResponse.IsValid)
            {
                _logger.LogError("Failed to create index: {Error}", createResponse.ServerError);
                throw new Exception($"Failed to create index: {createResponse.ServerError}");
            }
        }
    }

    public async Task IndexDocumentAsync(DocumentIndexModel document)
    {
        _logger.LogInformation("Indexing document {DocumentId}", document.DocumentId);
        
        var response = await _client.IndexDocumentAsync(document);
        
        if (!response.IsValid)
        {
            _logger.LogError("Failed to index document {DocumentId}: {Error}", 
                document.DocumentId, response.ServerError);
            throw new Exception($"Failed to index document: {response.ServerError}");
        }
    }

    public async Task<SearchResult> SearchDocumentsAsync(string query, int from = 0, int size = 10)
    {
        _logger.LogInformation("Searching for: {Query}", query);
        
        var searchResponse = await _client.SearchAsync<DocumentIndexModel>(s => s
            .From(from)
            .Size(size)
            .Query(q => q
                .MultiMatch(m => m
                    .Query(query)
                    .Fields(f => f
                        .Field(p => p.ExtractedText, boost: 2.0)
                        .Field(p => p.FileName, boost: 1.5)
                        .Field(p => p.Summary)
                        .Field(p => p.Tags)
                    )
                    .Fuzziness(Fuzziness.Auto)
                )
            )
            .Highlight(h => h
                .Fields(
                    f => f.Field(p => p.ExtractedText).PreTags("<mark>").PostTags("</mark>"),
                    f => f.Field(p => p.FileName).PreTags("<mark>").PostTags("</mark>")
                )
            )
        );

        if (!searchResponse.IsValid)
        {
            _logger.LogError("Search failed: {Error}", searchResponse.ServerError);
            throw new Exception($"Search failed: {searchResponse.ServerError}");
        }

        return new SearchResult
        {
            TotalHits = searchResponse.Total,
            Documents = searchResponse.Hits.Select(h => new SearchResultDocument
            {
                DocumentId = h.Source.DocumentId,
                FileName = h.Source.FileName,
                ExtractedText = h.Source.ExtractedText,
                Summary = h.Source.Summary,
                Tags = h.Source.Tags,
                UploadedAt = h.Source.UploadedAt,
                Score = h.Score ?? 0,
                Highlights = h.Highlight
            }).ToList()
        };
    }

    public async Task DeleteDocumentAsync(Guid documentId)
    {
        _logger.LogInformation("Deleting document {DocumentId} from index", documentId);
        
        var response = await _client.DeleteByQueryAsync<DocumentIndexModel>(d => d
            .Query(q => q
                .Term(t => t.DocumentId, documentId)
            )
        );

        if (!response.IsValid)
        {
            _logger.LogError("Failed to delete document {DocumentId}: {Error}", 
                documentId, response.ServerError);
        }
    }
}