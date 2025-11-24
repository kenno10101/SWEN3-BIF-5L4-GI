using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SWEN_DMS.DAL;
using SWEN_DMS.DAL.Repositories;
using SWEN_DMS.BLL.Services;
using SWEN_DMS.BLL.Interfaces;
using SWEN_DMS.BLL.Messaging;
using FluentValidation;
using FluentValidation.AspNetCore;
using Minio;
using SWEN_DMS.Validators;
using SWEN_DMS.Middleware;


//comment to push develop branch

var builder = WebApplication.CreateBuilder(args);

// RabbitMQ-Config + Publisher
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

// Repository + Service
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<DocumentService>();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Controller + Swagger aktivieren
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database (PostgreSQL via EF Core)
builder.Services.AddValidatorsFromAssemblyContaining<DocumentCreateDtoValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

// Datenbank (PostgreSQL via EF Core)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// MinIO
var minioEndpoint = builder.Configuration["Minio:Endpoint"] ?? "minio";
var minioPort = int.Parse(builder.Configuration["Minio:Port"] ?? "9000");
var minioAccessKey = builder.Configuration["Minio:AccessKey"] ?? "kendi";
var minioSecretKey = builder.Configuration["Minio:SecretKey"] ?? "kendi123";
var minioBucket = builder.Configuration["Minio:Bucket"] ?? "documents";

builder.Services.AddSingleton<IMinioClient>(sp =>
    new MinioClient()
        .WithEndpoint(minioEndpoint, minioPort)
        .WithCredentials(minioAccessKey, minioSecretKey)
        .WithSSL(false)
        .Build()
);

builder.Services.AddSingleton(sp => minioBucket);


// Debug
Console.WriteLine("Connection String in use: " + builder.Configuration.GetConnectionString("DefaultConnection"));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebClients", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "http://localhost:8085"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Swagger only in DEV
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Exception Handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// no HTTPS-Redirect in container
// app.UseHttpsRedirection();

app.UseAuthorization();
app.UseCors("AllowWebClients");

// ---- Health (minimal) ----
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// ---- MQ-Health (TCP-Port-Check) ----
app.MapGet("/health/mq", async (IOptions<RabbitMqOptions> opts) =>
{
    var o = opts.Value;
    try
    {
        using var client = new TcpClient();
        var connectTask = client.ConnectAsync(o.Host, o.Port);
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2));
        var finished = await Task.WhenAny(connectTask, timeoutTask);
        if (finished != connectTask || !client.Connected)
            return Results.Problem(statusCode: 503, title: "mq-unhealthy", detail: "TCP connect timeout");

        return Results.Ok(new { mq = "ok" });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 503, title: "mq-unhealthy");
    }
});

// MQ-Testendpoint (only DEV)
app.MapPost("/_mq/test", async (IMessagePublisher publisher) =>
{
    var msg = new { Type = "ping", TimeUtc = DateTime.UtcNow };
    await publisher.PublishAsync(msg);
    return Results.Ok(new { sent = true });
});

app.MapControllers();

app.Run();
