namespace MediaGrabber.Models;

public class DownloadJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Thumbnail { get; set; }
    public string? Duration { get; set; }
    public string Platform { get; set; } = "";

    public string FormatSelector { get; set; } = "bestvideo+bestaudio/best";
    public string OutputDirectory { get; set; } = "";
    public string? OutputFilename { get; set; }

    public bool EmbedSubtitles { get; set; }
    public bool EmbedMetadata { get; set; }
    public bool ExtractAudio { get; set; }
    public string? AudioFormat { get; set; } // mp3, opus, m4a, etc.

    public FFmpegOptions? PostProcessOptions { get; set; }

    public JobStatus Status { get; set; } = JobStatus.Pending;
    public double Progress { get; set; }
    public string? Speed { get; set; }
    public string? Eta { get; set; }
    public string? CurrentOperation { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OutputPath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public List<LogEntry> Logs { get; set; } = new();

    public CancellationTokenSource? CancellationTokenSource { get; set; }

    public bool CanCancel => Status == JobStatus.Pending || Status == JobStatus.Downloading || Status == JobStatus.Processing;
    public bool CanRetry => Status == JobStatus.Failed || Status == JobStatus.Cancelled;

    public TimeSpan? ElapsedTime
    {
        get
        {
            if (!StartedAt.HasValue) return null;
            var endTime = CompletedAt ?? DateTime.Now;
            return endTime - StartedAt.Value;
        }
    }

    public void AddLog(string message, LogLevel level = LogLevel.Info)
    {
        Logs.Add(new LogEntry
        {
            Timestamp = DateTime.Now,
            Message = message,
            Level = level
        });
    }
}

public enum JobStatus
{
    Pending,
    Downloading,
    Processing,
    Completed,
    Failed,
    Cancelled,
    Paused
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = "";
    public LogLevel Level { get; set; }

    public string TimestampString => Timestamp.ToString("HH:mm:ss");
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}
