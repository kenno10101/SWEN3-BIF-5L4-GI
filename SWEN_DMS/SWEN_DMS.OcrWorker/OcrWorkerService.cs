using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Minio;
using Minio.DataModel.Args;
using SWEN_DMS.DTOs.Messages;

namespace SWEN_DMS.OcrWorker;

public class OcrWorkerService
{
    private readonly IConnectionFactory _factory;
    private readonly string _exchange;
    private readonly string _queue;
    private readonly string _routingKey;
    private readonly IMinioClient _minioClient;
    private readonly string _bucket;

    public OcrWorkerService(
        IConnectionFactory factory,
        string exchange,
        string queue,
        string routingKey,
        IMinioClient minioClient,
        string bucket)
    {
        _factory = factory;
        _exchange = exchange;
        _queue = queue;
        _routingKey = routingKey;
        _minioClient = minioClient;
        _bucket = bucket;
    }

    public void Start()
    {
        using var connection = _factory.CreateConnection("swen-dms-ocr-worker");
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(_exchange, ExchangeType.Direct, true, false);
        channel.QueueDeclare(_queue, true, false, false);
        channel.QueueBind(_queue, _exchange, _routingKey);

        Console.WriteLine($"[OCR Worker] Listening on {_queue} ...");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var msg = JsonSerializer.Deserialize<OcrRequestMessage>(json);

            try
            {
                Console.WriteLine($"[OCR Worker] Processing document {msg.DocumentId}");

                // 1. Fetch PDF from MinIO
                using var pdfStream = new MemoryStream();
                await _minioClient.GetObjectAsync(new GetObjectArgs()
                    .WithBucket(_bucket)
                    .WithObject(msg.PdfKey)
                    .WithCallbackStream(s => s.CopyTo(pdfStream))
                );
                pdfStream.Position = 0;

                // 2. Run OCR
                string extractedText = RunOcr(pdfStream);

                // 3. Save to DB (you would inject a service/repository here)
                Console.WriteLine($"[OCR Worker] Extracted text length: {extractedText.Length}");

                // 4. ACK message
                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OCR Worker] Error: {ex.Message}");
                channel.BasicNack(ea.DeliveryTag, false, requeue: false);
            }
        };

        channel.BasicQos(0, 1, false);
        channel.BasicConsume(_queue, false, consumer);

        // keep running
        Task.Delay(-1).Wait();
    }

    private string RunOcr(Stream pdfStream)
    {
        // TODO: integrate Tesseract or Ghostscript
        // return dummy text for now
        return "OCR text placeholder";
    }

}