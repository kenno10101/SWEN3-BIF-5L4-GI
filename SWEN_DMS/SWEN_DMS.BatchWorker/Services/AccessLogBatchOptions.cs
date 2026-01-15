namespace SWEN_DMS.BatchWorker.Services;

public class AccessLogBatchOptions
{
    public string InputFolder { get; set; } = "/accesslogs/in";
    public string ArchiveFolder { get; set; } = "/accesslogs/archive";
    public string FilePattern { get; set; } = "access_*.xml";

    // tägliche Uhrzeit (UTC) z.B. 01:00
    public int RunHourUtc { get; set; } = 1;
    public int RunMinuteUtc { get; set; } = 0;

    // Optional: sofort beim Start einmal laufen lassen (praktisch fürs Demo)
    public bool RunOnStartup { get; set; } = true;
}