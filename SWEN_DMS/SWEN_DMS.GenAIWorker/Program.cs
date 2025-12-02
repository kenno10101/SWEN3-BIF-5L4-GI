using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SWEN_DMS.DTOs;
using SWEN_DMS.DTOs.Messages;

// --- ENV ---
string host   = Environment.GetEnvironmentVariable("RabbitMq__Host") ?? "rabbitmq";
int    port   = int.TryParse(Environment.GetEnvironmentVariable("RabbitMq__Port"), out var p) ? p : 5672;
string user   = Environment.GetEnvironmentVariable("RabbitMq__User") ?? "kendi";
string pass   = Environment.GetEnvironmentVariable("RabbitMq__Password") ?? "kendi";
string vhost  = Environment.GetEnvironmentVariable("RabbitMq__VirtualHost") ?? "/";

string exchange   = Environment.GetEnvironmentVariable("RabbitMq__Exchange") ?? "dms.exchange";
string queue      = Environment.GetEnvironmentVariable("RabbitMq__Queue") ?? "genai.requests";
string routingKey = Environment.GetEnvironmentVariable("RabbitMq__RoutingKey") ?? "genai.request";

// GenAI config 
string provider = Environment.GetEnvironmentVariable("GENAI_PROVIDER") ?? "google";
string model    = Environment.GetEnvironmentVariable("GENAI_MODEL")    ?? "gemini-1.5-flash";
string apiKey   = Environment.GetEnvironmentVariable("GENAI_API_KEY")  ?? "";

string restBase = Environment.GetEnvironmentVariable("Rest__BaseUrl") ?? "http://rest-server:8080";

// --- Guards / Diagnostics ---
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("[GenAI Worker] WARNING: GENAI_API_KEY is empty. Summarization will fail with 401.");
}
Console.WriteLine($"[GenAI Worker] Using provider={provider}, model={model}, restBase={restBase}");

// --- RabbitMQ factory ---
var factory = new ConnectionFactory
{
    HostName = host,
    Port     = port,
    UserName = user,
    Password = pass,
    VirtualHost = vhost,

    // optional: auto-recovery bei kurzen Netzproblemen
    AutomaticRecoveryEnabled = true,
    NetworkRecoveryInterval  = TimeSpan.FromSeconds(5)
};

// **robuste Verbindung mit Retry**
using var conn = ConnectWithRetry(factory, "swen-dms-genai-worker", maxAttempts: 12);
using var ch   = conn.CreateModel();

// Topology (idempotent)
ch.ExchangeDeclare(exchange, ExchangeType.Direct, durable: true, autoDelete: false);
ch.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);
ch.QueueBind(queue, exchange, routingKey);

Console.WriteLine($"[GenAI Worker] Listening on '{queue}' (model={model}, provider={provider}) ...");

// --- HTTP ---
var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

var consumer = new EventingBasicConsumer(ch);
consumer.Received += async (_, ea) =>
{
    GenAiRequestMessage? msg = null;
    try
    {
        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
        msg = JsonSerializer.Deserialize<GenAiRequestMessage>(json);

        if (msg is null)
            throw new InvalidOperationException("Message deserialization returned null.");

        var textLen = msg.Text?.Length ?? 0;
        Console.WriteLine($"[GenAI Worker] Got request for Document {msg.DocumentId} (textLen={textLen})");

        if (textLen == 0)
            throw new InvalidOperationException("Incoming GenAI request has empty text.");

        if (provider != "google")
            throw new NotSupportedException($"Provider '{provider}' not supported in this worker.");

        // (1) Summarize via Gemini
        var summary = await SummarizeWithGeminiAsync(http, apiKey, model, msg.Text!);
        Console.WriteLine($"[GenAI Worker] Gemini summary length: {summary.Length}");

        // (2) REST: Summary speichern
        var dto  = new DocumentSummaryUpdateDto { Summary = summary };
        var body = JsonSerializer.Serialize(dto);
        var url  = $"{restBase}/api/document/{msg.DocumentId}/summary";

        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var put = await http.PutAsync(url, content);

        var respText = await put.Content.ReadAsStringAsync();
        Console.WriteLine($"[GenAI Worker] REST PUT {url} -> {(int)put.StatusCode} {put.StatusCode}; body='{respText}'");

        put.EnsureSuccessStatusCode(); // wirft bei 4xx/5xx

        Console.WriteLine($"[GenAI Worker] summary saved for {msg.DocumentId}");
        ch.BasicAck(ea.DeliveryTag, false);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[GenAI Worker] Error: {ex}");
        ch.BasicNack(ea.DeliveryTag, false, requeue: false);
    }
};

ch.BasicQos(0, 1, false);
ch.BasicConsume(queue, autoAck: false, consumer: consumer);
await Task.Delay(-1);

// --- Helpers ---

static IConnection ConnectWithRetry(ConnectionFactory factory, string clientName, int maxAttempts = 10)
{
    var delay = TimeSpan.FromSeconds(2);
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            Console.WriteLine($"[GenAI Worker] Connecting to RabbitMQ (attempt {attempt}/{maxAttempts}) …");
            var conn = factory.CreateConnection(clientName);
            Console.WriteLine("[GenAI Worker] Connected to RabbitMQ.");
            return conn;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GenAI Worker] Connect failed: {ex.Message}");
            if (attempt == maxAttempts) throw;
            Thread.Sleep(delay);
            // leichter Backoff
            var next = delay.TotalSeconds * 1.5;
            delay = TimeSpan.FromSeconds(next > 15 ? 15 : next);
        }
    }
    throw new InvalidOperationException("Unreachable code");
}

static async Task<string> SummarizeWithGeminiAsync(HttpClient http, string apiKey, string model, string text)
{
    // Google Generative Language API endpoint
    var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(apiKey)}";

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

    Console.WriteLine($"[GenAI Worker] Calling Gemini: model={model}, endpoint=/v1beta/... (key set={(string.IsNullOrWhiteSpace(apiKey) ? "no" : "yes")})");
    var resp = await http.PostAsync(endpoint, content);

    var payload = await resp.Content.ReadAsStringAsync();
    Console.WriteLine($"[GenAI Worker] Gemini HTTP {(int)resp.StatusCode} {resp.StatusCode}; rawLen={payload.Length}");

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
        summary = "(empty summary)";

    return summary;
}
