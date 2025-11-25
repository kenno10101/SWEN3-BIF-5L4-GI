using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Minio;
using Minio.DataModel.Args;
using SWEN_DMS.DTOs.Messages;
using SWEN_DMS.OcrWorker;

string host = Environment.GetEnvironmentVariable("RabbitMq__Host") ?? "rabbitmq";
int    port = int.TryParse(Environment.GetEnvironmentVariable("RabbitMq__Port"), out var p) ? p : 5672;
string user = Environment.GetEnvironmentVariable("RabbitMq__User") ?? "kendi";
string pass = Environment.GetEnvironmentVariable("RabbitMq__Password") ?? "kendi";
string vhost= Environment.GetEnvironmentVariable("RabbitMq__VirtualHost") ?? "/";

string exchange   = Environment.GetEnvironmentVariable("RabbitMq__Exchange") ?? "dms.exchange";
string queue      = Environment.GetEnvironmentVariable("RabbitMq__Queue") ?? "ocr.requests";
string routingKey = Environment.GetEnvironmentVariable("RabbitMq__RoutingKey") ?? "ocr.request";

string minioEndpoint = Environment.GetEnvironmentVariable("Minio__Endpoint") ?? "minio";
int minioPort = int.Parse(Environment.GetEnvironmentVariable("Minio__Port") ?? "9000");
string minioAccessKey = Environment.GetEnvironmentVariable("Minio__AccessKey") ?? "kendi";
string minioSecretKey = Environment.GetEnvironmentVariable("Minio__SecretKey") ?? "kendi123";
string minioBucket = Environment.GetEnvironmentVariable("Minio__Bucket") ?? "documents";
var minioClient = new MinioClient()
    .WithEndpoint(minioEndpoint, minioPort)
    .WithCredentials(minioAccessKey, minioSecretKey)
    .WithSSL(minioEndpoint.StartsWith("https"))
    .Build();

var factory = new ConnectionFactory
{
    HostName = host,
    Port     = port,
    UserName = user,
    Password = pass,
    VirtualHost = vhost
};

using var connection = factory.CreateConnection("swen-dms-ocr-worker");
using var channel    = connection.CreateModel();

// Topologie sicherstellen (safe wenn schon vorhanden)
channel.ExchangeDeclare(exchange, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
channel.QueueBind(queue, exchange, routingKey);

Console.WriteLine($"[Worker] listening on '{queue}' ...");

var consumer = new EventingBasicConsumer(channel);
consumer.Received += async (model, ea) =>
{
    try
    {
        var body = ea.Body.ToArray();
        var json = Encoding.UTF8.GetString(body);
        Console.WriteLine($"[Worker] message: {json}");
        
        var msg = JsonSerializer.Deserialize<OcrRequestMessage>(json);
        Console.WriteLine($"[Worker] received OCR job for Document {msg.DocumentId}");
        
        string tempPath = Path.Combine(Path.GetTempPath(), msg.PdfKey);

        try
        {
            await minioClient.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(minioBucket)
                    .WithObject(msg.PdfKey)
                    .WithCallbackStream(async stream =>
                    {
                        using var fs = File.Create(tempPath);
                        await stream.CopyToAsync(fs);
                    })
            );
            Console.WriteLine($"[Worker] downloaded {msg.PdfKey} to {tempPath}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Worker] Failed to download '{msg.PdfKey}' from MinIO: {e.Message}");
            throw;
        }

        // TODO (später): 2. echte OCR hier
        // TODO 3: update extracted text in DB (create new update endpoint in rest api)
        // jetzt nur ACK:
        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Worker] error: {ex.Message}");
        // requeue=false -> DeadLetter (wenn konfiguriert) / drop:
        channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
    }
};

channel.BasicQos(0, 1, false); // fair dispatch
channel.BasicConsume(queue: queue, autoAck: false, consumer: consumer);

// Blockieren
await Task.Delay(-1);
