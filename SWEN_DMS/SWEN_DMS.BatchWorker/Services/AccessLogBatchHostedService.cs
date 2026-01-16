using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SWEN_DMS.BatchWorker.Services;

public class AccessLogBatchHostedService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<AccessLogBatchHostedService> _logger;
    private readonly AccessLogBatchOptions _opt;

    public AccessLogBatchHostedService(
        IServiceProvider sp,
        ILogger<AccessLogBatchHostedService> logger,
        IOptions<AccessLogBatchOptions> opt)
    {
        _sp = sp;
        _logger = logger;
        _opt = opt.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(_opt.InputFolder);
        Directory.CreateDirectory(_opt.ArchiveFolder);

        if (_opt.RunOnStartup)
        {
            await RunBatchOnce(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRunUtc(_opt.RunHourUtc, _opt.RunMinuteUtc);
            _logger.LogInformation("Next batch run in {Delay}", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            await RunBatchOnce(stoppingToken);
        }
    }

    private async Task RunBatchOnce(CancellationToken ct)
    {
        _logger.LogInformation("Starting access log batch run...");

        var files = Directory.GetFiles(_opt.InputFolder, _opt.FilePattern);
        if (files.Length == 0)
        {
            _logger.LogInformation("No files found in {Folder} matching {Pattern}", _opt.InputFolder, _opt.FilePattern);
            return;
        }

        foreach (var file in files.OrderBy(f => f))
        {
            try
            {
                using var scope = _sp.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<AccessLogXmlProcessor>();

                await processor.ProcessFileAsync(file, ct);

                ArchiveFile(file);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed processing file {File}. Leaving it in input folder for retry.", file);
                // bewusst nicht archivieren, damit Retry möglich ist
            }
        }

        _logger.LogInformation("Batch run finished.");
    }

    private void ArchiveFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var destName = $"{Path.GetFileNameWithoutExtension(fileName)}_{stamp}{Path.GetExtension(fileName)}";
        var destPath = Path.Combine(_opt.ArchiveFolder, destName);

        File.Move(filePath, destPath);
        _logger.LogInformation("Archived {File} -> {Archived}", fileName, destName);
    }

    private static TimeSpan GetDelayUntilNextRunUtc(int hourUtc, int minuteUtc)
    {
        var now = DateTime.UtcNow;
        var next = new DateTime(now.Year, now.Month, now.Day, hourUtc, minuteUtc, 0, DateTimeKind.Utc);
        if (next <= now) next = next.AddDays(1);
        return next - now;
    }
}
