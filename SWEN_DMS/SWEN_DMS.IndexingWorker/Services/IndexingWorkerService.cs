using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SWEN_DMS.IndexingWorker.Models;
using SWEN_DMS.DTOs.Messages;

namespace SWEN_DMS.IndexingWorker.Services;

public class IndexingWorkerService : BackgroundService
{
    private readonly ILogger<IndexingWorkerService> _logger;
    private readonly IElasticsearchService _elasticsearchService;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;

    public IndexingWorkerService(
        ILogger<IndexingWorkerService> logger,
        IElasticsearchService elasticsearchService,
        IConfiguration configuration)
    {
        _logger = logger;
        _elasticsearchService = elasticsearchService;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    await Task.Delay(5000, stoppingToken); // Wait for RabbitMQ to be ready
    
    SetupRabbitMq();
    
    var consumer = new EventingBasicConsumer(_channel);
    consumer.Received += async (model, ea) =>
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation("Received indexing request: {Message}", message);

            // Parse the JSON to determine message type
            var jsonDoc = JsonDocument.Parse(message);
            
            // Check if it's a delete message
            if (jsonDoc.RootElement.TryGetProperty("Action", out var actionElement) && 
                actionElement.GetString() == "delete")
            {
                var deleteMsg = JsonSerializer.Deserialize<DeleteDocumentMessage>(message);
                if (deleteMsg != null)
                {
                    await ProcessDeleteRequest(deleteMsg.DocumentId);
                }
            }
            else
            {
                // Regular indexing request
                var indexMsg = JsonSerializer.Deserialize<IndexingRequest>(message);
                if (indexMsg != null)
                {
                    await ProcessIndexingRequest(indexMsg);
                }
            }
            
            _channel?.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing indexing request");
            _channel?.BasicNack(ea.DeliveryTag, false, true);
        }
    };

    var queue = _configuration["RabbitMq:Queue"] ?? "indexing.requests";
    _channel?.BasicConsume(queue: queue, autoAck: false, consumer: consumer);
    
    _logger.LogInformation("Indexing worker started");
    
    await Task.Delay(Timeout.Infinite, stoppingToken);
}

private async Task ProcessIndexingRequest(IndexingRequest request)
{
    var document = new DocumentIndexModel
    {
        DocumentId = request.DocumentId,
        FileName = request.FileName,
        ExtractedText = request.ExtractedText,
        Summary = request.Summary,
        Tags = request.Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim()).ToList() ?? new List<string>(),
        UploadedAt = request.UploadedAt
    };

    await _elasticsearchService.IndexDocumentAsync(document);
    _logger.LogInformation("Document {DocumentId} indexed successfully", request.DocumentId);
}

private async Task ProcessDeleteRequest(Guid documentId)
{
    _logger.LogInformation("Processing delete request for document {DocumentId}", documentId);
    await _elasticsearchService.DeleteDocumentAsync(documentId);
    _logger.LogInformation("Document {DocumentId} deleted from Elasticsearch", documentId);
}
    private void SetupRabbitMq()
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:Host"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMq:Port"] ?? "5672"),
            UserName = _configuration["RabbitMq:User"] ?? "guest",
            Password = _configuration["RabbitMq:Password"] ?? "guest",
            VirtualHost = _configuration["RabbitMq:VirtualHost"] ?? "/"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        var exchange = _configuration["RabbitMq:Exchange"] ?? "dms.exchange";
        var queue = _configuration["RabbitMq:Queue"] ?? "indexing.requests";
        var routingKey = _configuration["RabbitMq:RoutingKey"] ?? "indexing.request";

        _channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Direct, durable: true);
        _channel.QueueDeclare(queue: queue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue: queue, exchange: exchange, routingKey: routingKey);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}

public class IndexingRequest
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ExtractedText { get; set; }
    public string? Summary { get; set; }
    public string? Tags { get; set; }
    public DateTime UploadedAt { get; set; }
}
