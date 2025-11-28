using Minio;
using SWEN_DMS.OcrWorker;
using RabbitMQ.Client;

// RabbitMQ aus ENV
string host   = Environment.GetEnvironmentVariable("RabbitMq__Host") ?? "rabbitmq";
int    port   = int.TryParse(Environment.GetEnvironmentVariable("RabbitMq__Port"), out var p) ? p : 5672;
string user   = Environment.GetEnvironmentVariable("RabbitMq__User") ?? "kendi";
string pass   = Environment.GetEnvironmentVariable("RabbitMq__Password") ?? "kendi";
string vhost  = Environment.GetEnvironmentVariable("RabbitMq__VirtualHost") ?? "/";
string exch   = Environment.GetEnvironmentVariable("RabbitMq__Exchange") ?? "dms.exchange";
string queue  = Environment.GetEnvironmentVariable("RabbitMq__Queue") ?? "ocr.requests";
string rkey   = Environment.GetEnvironmentVariable("RabbitMq__RoutingKey") ?? "ocr.request";

// MinIO aus ENV
string endpoint = Environment.GetEnvironmentVariable("Minio__Endpoint") ?? "minio";
int    mPort    = int.TryParse(Environment.GetEnvironmentVariable("Minio__Port"), out var mp) ? mp : 9000;
string accKey   = Environment.GetEnvironmentVariable("Minio__AccessKey") ?? "kendi";
string secKey   = Environment.GetEnvironmentVariable("Minio__SecretKey") ?? "kendi123";
string bucket   = Environment.GetEnvironmentVariable("Minio__Bucket") ?? "documents";

string restBase = Environment.GetEnvironmentVariable("REST__BaseUrl") ?? "http://rest-server:8080";


// Clients
var factory = new ConnectionFactory { HostName = host, Port = port, UserName = user, Password = pass, VirtualHost = vhost };

var minio = new MinioClient()
    .WithEndpoint(endpoint, mPort)
    .WithCredentials(accKey, secKey)
    .WithSSL(endpoint.StartsWith("https"))
    .Build();

// Bucket einmalig sicherstellen
try
{
    var found = await minio.BucketExistsAsync(new Minio.DataModel.Args.BucketExistsArgs().WithBucket(bucket));
    if (!found)
        await minio.MakeBucketAsync(new Minio.DataModel.Args.MakeBucketArgs().WithBucket(bucket));
}
catch (Exception ex)
{
    Console.WriteLine($"[Worker] Bucket check/create failed: {ex.Message}");
}

// Service starten
var service = new OcrWorkerService(factory, exch, queue, rkey, minio, bucket, restBase);
service.Start();
