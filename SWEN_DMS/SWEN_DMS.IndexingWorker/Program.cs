using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nest;
using SWEN_DMS.IndexingWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Elasticsearch configuration
var elasticsearchUri = builder.Configuration["Elasticsearch:Uri"] ?? "http://localhost:9200";
var indexName = builder.Configuration["Elasticsearch:IndexName"] ?? "documents";

var settings = new ConnectionSettings(new Uri(elasticsearchUri))
    .DefaultIndex(indexName)
    .EnableDebugMode()
    .PrettyJson();

var client = new ElasticClient(settings);

builder.Services.AddSingleton<IElasticClient>(client);
builder.Services.AddSingleton<IElasticsearchService, ElasticsearchService>();
builder.Services.AddHostedService<IndexingWorkerService>();

var app = builder.Build();

// Create index if it doesn't exist
var elasticService = app.Services.GetRequiredService<IElasticsearchService>();
await elasticService.EnsureIndexExistsAsync();

app.Run();