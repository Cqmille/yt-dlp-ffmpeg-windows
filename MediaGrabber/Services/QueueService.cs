using System.Collections.Concurrent;
using MediaGrabber.Models;

namespace MediaGrabber.Services;

public class QueueService : BackgroundService
{
    private readonly ConfigService _configService;
    private readonly YtDlpService _ytDlpService;
    private readonly FFmpegService _ffmpegService;
    private readonly ILogger<QueueService> _logger;

    private readonly ConcurrentQueue<DownloadJob> _pendingJobs = new();
    private readonly ConcurrentDictionary<string, DownloadJob> _allJobs = new();
    private readonly SemaphoreSlim _semaphore;

    public event Action? OnJobsChanged;
    public event Action<DownloadJob>? OnJobUpdated;

    public QueueService(
        ConfigService configService,
        YtDlpService ytDlpService,
        FFmpegService ffmpegService,
        ILogger<QueueService> logger)
    {
        _configService = configService;
        _ytDlpService = ytDlpService;
        _ffmpegService = ffmpegService;
        _logger = logger;
        _semaphore = new SemaphoreSlim(_configService.Config.MaxConcurrentDownloads);
    }

    public IEnumerable<DownloadJob> AllJobs => _allJobs.Values.OrderByDescending(j => j.CreatedAt);
    public IEnumerable<DownloadJob> PendingJobs => _allJobs.Values.Where(j => j.Status == JobStatus.Pending);
    public IEnumerable<DownloadJob> ActiveJobs => _allJobs.Values.Where(j => j.Status == JobStatus.Downloading || j.Status == JobStatus.Processing);
    public IEnumerable<DownloadJob> CompletedJobs => _allJobs.Values.Where(j => j.Status == JobStatus.Completed);
    public IEnumerable<DownloadJob> FailedJobs => _allJobs.Values.Where(j => j.Status == JobStatus.Failed || j.Status == JobStatus.Cancelled);

    public int TotalCount => _allJobs.Count;
    public int PendingCount => _allJobs.Values.Count(j => j.Status == JobStatus.Pending);
    public int ActiveCount => _allJobs.Values.Count(j => j.Status == JobStatus.Downloading || j.Status == JobStatus.Processing);
    public int CompletedCount => _allJobs.Values.Count(j => j.Status == JobStatus.Completed);
    public int FailedCount => _allJobs.Values.Count(j => j.Status == JobStatus.Failed);

    public DownloadJob? GetJob(string id) => _allJobs.TryGetValue(id, out var job) ? job : null;

    public DownloadJob EnqueueDownload(
        VideoMetadata metadata,
        string formatSelector,
        bool embedSubtitles = true,
        bool embedMetadata = true,
        bool extractAudio = false,
        string? audioFormat = null,
        FFmpegOptions? postProcessOptions = null)
    {
        var job = new DownloadJob
        {
            Url = metadata.WebpageUrl ?? "",
            Title = metadata.Title,
            Thumbnail = metadata.Thumbnail,
            Duration = metadata.FormattedDuration,
            Platform = metadata.PlatformName,
            FormatSelector = formatSelector,
            OutputDirectory = _configService.Config.OutputDirectory,
            EmbedSubtitles = embedSubtitles,
            EmbedMetadata = embedMetadata,
            ExtractAudio = extractAudio,
            AudioFormat = audioFormat,
            PostProcessOptions = postProcessOptions,
            Status = JobStatus.Pending
        };

        job.AddLog($"Job created for: {metadata.Title}");

        _allJobs[job.Id] = job;
        _pendingJobs.Enqueue(job);

        OnJobsChanged?.Invoke();
        return job;
    }

    public DownloadJob EnqueueFromUrl(string url, string formatSelector = "bestvideo+bestaudio/best")
    {
        var job = new DownloadJob
        {
            Url = url,
            Title = "Fetching...",
            FormatSelector = formatSelector,
            OutputDirectory = _configService.Config.OutputDirectory,
            EmbedSubtitles = _configService.Config.EmbedSubtitles,
            EmbedMetadata = _configService.Config.EmbedMetadata,
            Status = JobStatus.Pending
        };

        job.AddLog($"Job created for URL: {url}");

        _allJobs[job.Id] = job;
        _pendingJobs.Enqueue(job);

        OnJobsChanged?.Invoke();
        return job;
    }

    public void CancelJob(string jobId)
    {
        if (_allJobs.TryGetValue(jobId, out var job))
        {
            job.CancellationTokenSource?.Cancel();
            if (job.Status == JobStatus.Pending)
            {
                job.Status = JobStatus.Cancelled;
                job.AddLog("Job cancelled by user", JobLogLevel.Warning);
            }
            OnJobUpdated?.Invoke(job);
            OnJobsChanged?.Invoke();
        }
    }

    public void RetryJob(string jobId)
    {
        if (_allJobs.TryGetValue(jobId, out var job) && job.CanRetry)
        {
            job.Status = JobStatus.Pending;
            job.Progress = 0;
            job.ErrorMessage = null;
            job.CancellationTokenSource = null;
            job.AddLog("Job queued for retry");

            _pendingJobs.Enqueue(job);
            OnJobUpdated?.Invoke(job);
            OnJobsChanged?.Invoke();
        }
    }

    public void RemoveJob(string jobId)
    {
        if (_allJobs.TryRemove(jobId, out var job))
        {
            job.CancellationTokenSource?.Cancel();
            OnJobsChanged?.Invoke();
        }
    }

    public void ClearCompleted()
    {
        var completed = _allJobs.Values
            .Where(j => j.Status == JobStatus.Completed || j.Status == JobStatus.Cancelled)
            .Select(j => j.Id)
            .ToList();

        foreach (var id in completed)
        {
            _allJobs.TryRemove(id, out _);
        }

        OnJobsChanged?.Invoke();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queue service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_pendingJobs.TryDequeue(out var job))
            {
                if (job.Status != JobStatus.Pending)
                    continue;

                await _semaphore.WaitAsync(stoppingToken);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessJobAsync(job, stoppingToken);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }, stoppingToken);
            }
            else
            {
                await Task.Delay(500, stoppingToken);
            }
        }
    }

    private async Task ProcessJobAsync(DownloadJob job, CancellationToken stoppingToken)
    {
        job.CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var cancellationToken = job.CancellationTokenSource.Token;

        try
        {
            job.Status = JobStatus.Downloading;
            job.StartedAt = DateTime.Now;
            job.CurrentOperation = "Starting download...";
            job.AddLog("Download started");
            OnJobUpdated?.Invoke(job);

            await _ytDlpService.DownloadAsync(
                job,
                onProgress: (progress, speed, eta) =>
                {
                    job.Progress = progress;
                    job.Speed = speed;
                    job.Eta = eta;
                    job.CurrentOperation = $"Downloading: {progress:F1}%";
                    OnJobUpdated?.Invoke(job);
                },
                cancellationToken: cancellationToken);

            // Post-processing with FFmpeg if needed
            if (job.PostProcessOptions != null && !string.IsNullOrEmpty(job.OutputPath) && File.Exists(job.OutputPath))
            {
                job.Status = JobStatus.Processing;
                job.CurrentOperation = "Post-processing with FFmpeg...";
                job.AddLog("Starting FFmpeg post-processing");
                OnJobUpdated?.Invoke(job);

                var outputExt = job.PostProcessOptions.Container ?? Path.GetExtension(job.OutputPath).TrimStart('.');
                var processedPath = Path.Combine(
                    Path.GetDirectoryName(job.OutputPath)!,
                    Path.GetFileNameWithoutExtension(job.OutputPath) + "_processed." + outputExt);

                await _ffmpegService.ProcessAsync(
                    job.OutputPath,
                    processedPath,
                    job.PostProcessOptions,
                    onProgress: (progress, _) =>
                    {
                        job.Progress = progress;
                        job.CurrentOperation = $"Processing: {progress:F1}%";
                        OnJobUpdated?.Invoke(job);
                    },
                    onLog: line => job.AddLog(line, JobLogLevel.Debug),
                    cancellationToken: cancellationToken);

                // Replace original with processed
                if (File.Exists(processedPath))
                {
                    try
                    {
                        File.Delete(job.OutputPath);
                        File.Move(processedPath, job.OutputPath.Replace(
                            Path.GetExtension(job.OutputPath),
                            "." + outputExt));
                    }
                    catch
                    {
                        job.OutputPath = processedPath;
                    }
                }
            }

            job.Status = JobStatus.Completed;
            job.Progress = 100;
            job.CompletedAt = DateTime.Now;
            job.CurrentOperation = "Completed";
            job.AddLog($"Download completed: {job.OutputPath}");
        }
        catch (OperationCanceledException)
        {
            job.Status = JobStatus.Cancelled;
            job.CurrentOperation = "Cancelled";
            job.AddLog("Job cancelled", JobLogLevel.Warning);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed", job.Id);
            job.Status = JobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CurrentOperation = "Failed";
            job.AddLog($"Error: {ex.Message}", JobLogLevel.Error);
        }
        finally
        {
            job.CompletedAt ??= DateTime.Now;
            OnJobUpdated?.Invoke(job);
            OnJobsChanged?.Invoke();
        }
    }
}
