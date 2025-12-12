using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SWEN_DMS.GenAiWorker;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger<GenAiWorkerService>();

string host   = Environment.GetEnvironmentVariable("RabbitMq__Host") ?? "rabbitmq";
int    port   = int.TryParse(Environment.GetEnvironmentVariable("RabbitMq__Port"), out var p) ? p : 5672;
string user   = Environment.GetEnvironmentVariable("RabbitMq__User") ?? "kendi";
string pass   = Environment.GetEnvironmentVariable("RabbitMq__Password") ?? "kendi";
string vhost  = Environment.GetEnvironmentVariable("RabbitMq__VirtualHost") ?? "/";

string exchange   = Environment.GetEnvironmentVariable("RabbitMq__Exchange") ?? "dms.exchange";
string queue      = Environment.GetEnvironmentVariable("RabbitMq__Queue") ?? "genai.requests";
string routingKey = Environment.GetEnvironmentVariable("RabbitMq__RoutingKey") ?? "genai.request";

string provider = Environment.GetEnvironmentVariable("GENAI_PROVIDER") ?? "google";
string model    = Environment.GetEnvironmentVariable("GENAI_MODEL")    ?? "gemini-1.5-flash";
string apiKey   = Environment.GetEnvironmentVariable("GENAI_API_KEY")  ?? "";

string restBase = Environment.GetEnvironmentVariable("Rest__BaseUrl") ?? "http://rest-server:8080";

var factory = new ConnectionFactory
{
    HostName = host,
    Port     = port,
    UserName = user,
    Password = pass,
    VirtualHost = vhost,
    AutomaticRecoveryEnabled = true,
    NetworkRecoveryInterval  = TimeSpan.FromSeconds(5)
};

var service = new GenAiWorkerService(
    logger,
    factory,
    exchange,
    queue,
    routingKey,
    provider,
    model,
    apiKey,
    restBase
);

service.Start();