using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Minio;
using Minio.DataModel.Args;
using SWEN_DMS.DTOs.Messages;
using System.Net.Http;
using System.Net.Http.Json;

namespace SWEN_DMS.OcrWorker;

public class OcrWorkerService
{
    private readonly ConnectionFactory _factory;
    private readonly string _exchange;
    private readonly string _queue;
    private readonly string _routingKey;
    private readonly IMinioClient _minioClient;
    private readonly string _bucket;

    // REST
    private readonly string _restBaseUrl;
    private readonly HttpClient _http = new();

    public OcrWorkerService(
        ConnectionFactory factory,
        string exchange,
        string queue,
        string routingKey,
        IMinioClient minioClient,
        string bucket,
        string restBaseUrl)
    {
        _factory = factory;
        _exchange = exchange;
        _queue = queue;
        _routingKey = routingKey;
        _minioClient = minioClient;
        _bucket = bucket;
        _restBaseUrl = restBaseUrl.TrimEnd('/');
    }

    public void Start()
    {
        using var connection = _factory.CreateConnection("swen-dms-ocr-worker");
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(_exchange, ExchangeType.Direct, durable: true, autoDelete: false);
        channel.QueueDeclare(_queue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(_queue, _exchange, _routingKey);

        Console.WriteLine($"[OCR Worker] Listening on '{_queue}' ...");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            OcrRequestMessage? msg = null;
            try
            {
                msg = JsonSerializer.Deserialize<OcrRequestMessage>(json)
                      ?? throw new InvalidOperationException("Deserialized OcrRequestMessage was null.");

                Console.WriteLine($"[OCR Worker] Processing document {msg.DocumentId}");

                // (1) PDF aus MinIO laden
                using var pdfStream = new MemoryStream();
                await _minioClient.GetObjectAsync(new GetObjectArgs()
                    .WithBucket(_bucket)
                    .WithObject(msg.PdfKey)
                    .WithCallbackStream(s => s.CopyTo(pdfStream))
                );
                pdfStream.Position = 0;

                // (2) OCR ausführen
                string extractedText = RunOcr(pdfStream);
                Console.WriteLine($"[OCR Worker] Extracted text length: {extractedText.Length}");

                // (3) An REST publizieren
                await SendOcrResultAsync(msg.DocumentId, extractedText);

                // (3b) ➜ An GenAI publizieren (JETZT WICHTIG!)
                PublishGenAiRequest(channel, msg.DocumentId, extractedText);

                await SendToIndexingQueue(channel, msg.DocumentId, msg.PdfKey, extractedText);

                // (4) ACK
                channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OCR Worker] Error for message '{msg?.PdfKey ?? "unknown"}': {ex.Message}");
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        channel.BasicQos(0, 1, false);
        channel.BasicConsume(_queue, autoAck: false, consumer);

        Task.Delay(-1).Wait();
    }

    // OCR-Ergebnis an REST schicken
    private async Task SendOcrResultAsync(Guid documentId, string text)
    {
        var url = $"{_restBaseUrl}/api/document/{documentId}/ocr-result";

        var payload = new { extractedText = text };
        var resp = await _http.PostAsJsonAsync(url, payload);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Failed to send OCR result to REST ({(int)resp.StatusCode}): {body}");
        }
    }

    // OCR -> GenAI: publish message in Queue 
    private void PublishGenAiRequest(IModel channel, Guid documentId, string extractedText)
    {
        var genAi = new GenAiRequestMessage
        {
            DocumentId = documentId,
            Text       = extractedText,
            Model      = Environment.GetEnvironmentVariable("GENAI_MODEL")
        };

        var gaExchange   = Environment.GetEnvironmentVariable("GenAI__Exchange")   ?? "dms.exchange";
        var gaRoutingKey = Environment.GetEnvironmentVariable("GenAI__RoutingKey") ?? "genai.request";
        var gaQueue      = Environment.GetEnvironmentVariable("GenAI__Queue")      ?? "genai.requests";

        channel.ExchangeDeclare(gaExchange, ExchangeType.Direct, durable: true, autoDelete: false);
        channel.QueueDeclare(gaQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(gaQueue, gaExchange, gaRoutingKey);

        var payload = JsonSerializer.SerializeToUtf8Bytes(genAi);
        var props   = channel.CreateBasicProperties();
        props.ContentType  = "application/json";
        props.DeliveryMode = 2; // persistent

        channel.BasicPublish(
            exchange: gaExchange,
            routingKey: gaRoutingKey,
            basicProperties: props,
            body: payload
        );

        Console.WriteLine($"[OCR Worker] forwarded text to GenAI for doc {documentId}");
    }

    private string RunOcr(Stream pdfStream)
    {
        var work = Path.Combine(Path.GetTempPath(), "ocr_" + Guid.NewGuid());
        Directory.CreateDirectory(work);

        var pdfPath = Path.Combine(work, "input.pdf");
        using (var fs = File.Create(pdfPath)) pdfStream.CopyTo(fs);

        // Sprache dynamisch aus ENV (Fallback: eng+deu)
        var lang = Environment.GetEnvironmentVariable("OCR__Lang");
        if (string.IsNullOrWhiteSpace(lang)) lang = "eng+deu";

        // 1) PDF -> PNGs (pdftoppm)
        Run("pdftoppm", $"-png -r 300 \"{pdfPath}\" output", work);

        // 2) Für jede Seite tesseract
        var pages = Directory.GetFiles(work, "output-*.png").OrderBy(s => s).ToArray();
        var parts = new List<string>();
        foreach (var page in pages)
            parts.Add(RunAndCapture("tesseract", $"\"{page}\" stdout -l {lang}", work));

        try { Directory.Delete(work, true); } catch { /* ignore */ }

        return string.Join(Environment.NewLine + "----" + Environment.NewLine, parts);
    }

    private static void Run(string file, string args, string cwd)
    {
        var p = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo(file, args)
            {
                WorkingDirectory = cwd,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            }
        };
        p.Start();
        p.WaitForExit();
        if (p.ExitCode != 0) throw new InvalidOperationException($"{file} failed: {p.StandardError.ReadToEnd()}");
    }

    private static string RunAndCapture(string file, string args, string cwd)
    {
        var p = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo(file, args)
            {
                WorkingDirectory = cwd,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            }
        };
        p.Start();
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0) throw new InvalidOperationException($"{file} failed: {p.StandardError.ReadToEnd()}");
        return output;
    }
    
    private async Task SendToIndexingQueue(IModel channel, Guid documentId, string fileName, string extractedText)
    {
        var indexingRequest = new
        {
            DocumentId = documentId,
            FileName = fileName,
            ExtractedText = extractedText,
            Summary = "", // Will be filled by GenAI worker
            Tags = "",
            UploadedAt = DateTime.UtcNow
        };

        var message = JsonSerializer.Serialize(indexingRequest);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;

        channel.BasicPublish(
            exchange: "dms.exchange",
            routingKey: "indexing.request",
            basicProperties: properties,
            body: body
        );

        Console.WriteLine($"Indexing request sent for document {documentId}");
    }
}
