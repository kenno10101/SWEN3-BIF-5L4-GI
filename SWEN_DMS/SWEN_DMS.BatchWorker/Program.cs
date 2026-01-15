using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SWEN_DMS.DAL;
using SWEN_DMS.DAL.Repositories;
using SWEN_DMS.BatchWorker.Services;
using Microsoft.Extensions.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// DB
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository
builder.Services.AddScoped<IAccessLogDailyRepository, AccessLogDailyRepository>();

// Options
builder.Services.Configure<AccessLogBatchOptions>(builder.Configuration.GetSection("AccessLogBatch"));

// Processor + HostedService
builder.Services.AddScoped<AccessLogXmlProcessor>();
builder.Services.AddHostedService<AccessLogBatchHostedService>();

var app = builder.Build();

// Migrations wie bei REST
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();