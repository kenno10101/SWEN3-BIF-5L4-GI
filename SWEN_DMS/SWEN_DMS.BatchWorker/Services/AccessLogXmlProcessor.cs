using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SWEN_DMS.DAL.Repositories;

namespace SWEN_DMS.BatchWorker.Services;

public class AccessLogXmlProcessor
{
    private readonly IAccessLogDailyRepository _repo;
    private readonly ILogger<AccessLogXmlProcessor> _logger;

    public AccessLogXmlProcessor(IAccessLogDailyRepository repo, ILogger<AccessLogXmlProcessor> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task ProcessFileAsync(string filePath, CancellationToken ct)
    {
        _logger.LogInformation("Processing access log file: {File}", filePath);

        var doc = XDocument.Load(filePath);
        var root = doc.Root ?? throw new InvalidOperationException("XML has no root element.");

        if (root.Name.LocalName != "accessStatistics")
            throw new InvalidOperationException("Root must be <accessStatistics>.");

        var dateAttr = root.Attribute("dateUtc")?.Value;
        if (string.IsNullOrWhiteSpace(dateAttr))
            throw new InvalidOperationException("Missing required attribute dateUtc.");

        if (!DateOnly.TryParse(dateAttr, out var dayUtc))
            throw new InvalidOperationException($"Invalid dateUtc: {dateAttr}");

        var entries = root.Elements("document").ToList();
        if (!entries.Any())
        {
            _logger.LogWarning("No <document> entries found in {File}", filePath);
            return;
        }

        foreach (var e in entries)
        {
            var idStr = e.Attribute("documentId")?.Value;
            var countStr = e.Attribute("count")?.Value;

            if (!Guid.TryParse(idStr, out var documentId))
            {
                _logger.LogWarning("Invalid documentId '{Id}' in {File}", idStr, filePath);
                continue;
            }

            if (!int.TryParse(countStr, out var count) || count < 0)
            {
                _logger.LogWarning("Invalid count '{Count}' for doc {DocId} in {File}", countStr, documentId, filePath);
                continue;
            }

            if (count == 0) continue;

            await _repo.UpsertIncrementAsync(documentId, dayUtc, count, ct);
        }

        _logger.LogInformation("Finished processing {File}", filePath);
    }
}
