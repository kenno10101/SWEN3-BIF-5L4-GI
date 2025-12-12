using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SWEN_DMS.DTOs;
using SWEN_DMS.DTOs.Messages;

namespace SWEN_DMS.GenAiWorker;

public class GenAiWorkerService
{
    private readonly ILogger<GenAiWorkerService> _logger;
    private readonly ConnectionFactory _factory;
    private readonly string _exchange;
    private readonly string _queue;
    private readonly string _routingKey;
    private readonly string _provider;
    private readonly string _model;
    private readonly string _apiKey;
    private readonly string _restBaseUrl;
    private readonly HttpClient _http;

    public GenAiWorkerService(
        ILogger<GenAiWorkerService> logger,
        ConnectionFactory factory,
        string exchange,
        string queue,
        string routingKey,
        string provider,
        string model,
        string apiKey,
        string restBaseUrl)
    {
        _logger = logger;
        _factory = factory;
        _exchange = exchange;
        _queue = queue;
        _routingKey = routingKey;
        _provider = provider;
        _model = model;
        _apiKey = apiKey;
        _restBaseUrl = restBaseUrl.TrimEnd('/');

        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Diagnostics
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("API key is empty. Summarization will fail with 401");
        }
        _logger.LogInformation("Initialized GenAI Worker with provider={Provider}, model={Model}, restBase={RestBaseUrl}", 
            _provider, _model, _restBaseUrl);
    }

    public void Start()
    {
        using var connection = ConnectWithRetry(_factory, "swen-dms-genai-worker", maxAttempts: 12);
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(_exchange, ExchangeType.Direct, durable: true, autoDelete: false);
        channel.QueueDeclare(_queue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(_queue, _exchange, _routingKey);

        _logger.LogInformation("Listening on queue '{Queue}' (model={Model}, provider={Provider})", 
            _queue, _model, _provider);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            GenAiRequestMessage? msg = null;
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                msg = JsonSerializer.Deserialize<GenAiRequestMessage>(json);

                if (msg is null)
                {
                    _logger.LogError("Message deserialization returned null");
                    throw new InvalidOperationException("Message deserialization returned null.");
                }

                var textLen = msg.Text?.Length ?? 0;
                _logger.LogInformation("Processing request for Document {DocumentId} (textLen={TextLength})", 
                    msg.DocumentId, textLen);

                if (textLen == 0)
                {
                    _logger.LogError("Incoming GenAI request has empty text for Document {DocumentId}", msg.DocumentId);
                    throw new InvalidOperationException("Incoming GenAI request has empty text.");
                }

                if (_provider != "google")
                {
                    _logger.LogError("Unsupported provider '{Provider}' for Document {DocumentId}", _provider, msg.DocumentId);
                    throw new NotSupportedException($"Provider '{_provider}' not supported in this worker.");
                }

                // (1) Summarize via Gemini
                var summary = await SummarizeWithGeminiAsync(msg.Text!);
                _logger.LogInformation("Generated summary for Document {DocumentId} (length={SummaryLength})", 
                    msg.DocumentId, summary.Length);

                // (2) REST: Summary speichern
                await SaveSummaryAsync(msg.DocumentId, summary);

                _logger.LogInformation("Successfully saved summary for Document {DocumentId}", msg.DocumentId);
                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                var docId = msg?.DocumentId.ToString() ?? "unknown";
                _logger.LogError(ex, "Error processing GenAI request for Document {DocumentId}", docId);
                channel.BasicNack(ea.DeliveryTag, false, requeue: false);
            }
        };

        channel.BasicQos(0, 1, false);
        channel.BasicConsume(_queue, autoAck: false, consumer: consumer);

        Task.Delay(-1).Wait();
    }

    private async Task<string> SummarizeWithGeminiAsync(string text)
    {
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={Uri.EscapeDataString(_apiKey)}";

        var req = new
        {
            contents = new[]
            {
                new {
                    parts = new[] {
                        new { text = $"Summarize the following document in 5–7 concise bullet points. Keep it faithful to the source.\n\n{text}" }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(req);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("Calling Gemini API with model={Model}, keyConfigured={KeyConfigured}", 
            _model, !string.IsNullOrWhiteSpace(_apiKey));
        
        var resp = await _http.PostAsync(endpoint, content);

        var payload = await resp.Content.ReadAsStringAsync();
        
        _logger.LogDebug("Gemini API response: StatusCode={StatusCode}, ResponseLength={ResponseLength}", 
            (int)resp.StatusCode, payload.Length);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API request failed with status {StatusCode}: {ResponseBody}", 
                (int)resp.StatusCode, payload);
        }

        resp.EnsureSuccessStatusCode();

        // Antwort extrahieren
        using var doc = JsonDocument.Parse(payload);
        var sb = new StringBuilder();

        if (doc.RootElement.TryGetProperty("candidates", out var candidates))
        {
            foreach (var cand in candidates.EnumerateArray())
            {
                if (cand.TryGetProperty("content", out var contentEl) &&
                    contentEl.TryGetProperty("parts", out var parts))
                {
                    foreach (var part in parts.EnumerateArray())
                    {
                        if (part.TryGetProperty("text", out var txtNode))
                            sb.AppendLine(txtNode.GetString());
                    }
                }
            }
        }

        var summary = sb.ToString().Trim();
        if (string.IsNullOrWhiteSpace(summary))
        {
            _logger.LogWarning("Gemini API returned empty summary, using placeholder");
            summary = "(empty summary)";
        }

        return summary;
    }

    private async Task SaveSummaryAsync(Guid documentId, string summary)
    {
        var dto = new DocumentSummaryUpdateDto { Summary = summary };
        var body = JsonSerializer.Serialize(dto);
        var url = $"{_restBaseUrl}/api/document/{documentId}/summary";

        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        
        _logger.LogDebug("Sending summary to REST API: PUT {Url}", url);
        
        var put = await _http.PutAsync(url, content);

        var respText = await put.Content.ReadAsStringAsync();
        
        if (put.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully updated summary via REST API for Document {DocumentId}", documentId);
        }
        else
        {
            _logger.LogError("REST API update failed with status {StatusCode}: {ResponseBody}", 
                (int)put.StatusCode, respText);
        }

        put.EnsureSuccessStatusCode(); // wirft bei 4xx/5xx
    }

    private IConnection ConnectWithRetry(ConnectionFactory factory, string clientName, int maxAttempts = 10)
    {
        var delay = TimeSpan.FromSeconds(2);
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                _logger.LogInformation("Connecting to RabbitMQ (attempt {Attempt}/{MaxAttempts})", attempt, maxAttempts);
                var conn = factory.CreateConnection(clientName);
                _logger.LogInformation("Successfully connected to RabbitMQ");
                return conn;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ connection attempt {Attempt}/{MaxAttempts} failed", attempt, maxAttempts);
                
                if (attempt == maxAttempts)
                {
                    _logger.LogCritical(ex, "Failed to connect to RabbitMQ after {MaxAttempts} attempts", maxAttempts);
                    throw;
                }
                
                Thread.Sleep(delay);
                // leichter Backoff
                var next = delay.TotalSeconds * 1.5;
                delay = TimeSpan.FromSeconds(next > 15 ? 15 : next);
            }
        }
        throw new InvalidOperationException("Unreachable code");
    }
}