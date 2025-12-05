using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MediaGrabber.Services;

public class UpdateService : IDisposable
{
    private readonly ConfigService _configService;
    private readonly ILogger<UpdateService> _logger;
    private readonly HttpClient _httpClient;

    public event Action<string>? OnStatusChanged;
    public event Action<double>? OnProgressChanged;

    public string? LatestYtDlpVersion { get; private set; }
    public string? LatestFfmpegVersion { get; private set; }
    public bool YtDlpUpdateAvailable { get; private set; }
    public bool FfmpegUpdateAvailable { get; private set; }
    public bool IsChecking { get; private set; }
    public bool IsDownloading { get; private set; }

    public UpdateService(ConfigService configService, ILogger<UpdateService> logger)
    {
        _configService = configService;
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "MediaGrabber/1.0");
    }

    public async Task CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        IsChecking = true;
        OnStatusChanged?.Invoke("Checking for updates...");

        try
        {
            await Task.WhenAll(
                CheckYtDlpUpdateAsync(cancellationToken),
                CheckFfmpegUpdateAsync(cancellationToken)
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check for updates");
        }
        finally
        {
            IsChecking = false;
            OnStatusChanged?.Invoke("");
        }
    }

    private async Task CheckYtDlpUpdateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<GitHubRelease>(
                "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest",
                cancellationToken);

            if (response != null)
            {
                LatestYtDlpVersion = response.TagName?.TrimStart('v') ?? response.TagName;
                var currentVersion = _configService.Config.YtDlpVersion;
                YtDlpUpdateAvailable = !string.IsNullOrEmpty(LatestYtDlpVersion) &&
                                        LatestYtDlpVersion != currentVersion;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check yt-dlp updates");
        }
    }

    private async Task CheckFfmpegUpdateAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Check gyan.dev for latest ffmpeg release
            // Using a simplified approach - in production you'd parse their release page
            LatestFfmpegVersion = "7.1"; // Placeholder - would need to scrape gyan.dev
            var currentVersion = _configService.Config.FfmpegVersion;
            FfmpegUpdateAvailable = !string.IsNullOrEmpty(LatestFfmpegVersion) &&
                                     LatestFfmpegVersion != currentVersion;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check ffmpeg updates");
        }
    }

    public async Task DownloadYtDlpAsync(CancellationToken cancellationToken = default)
    {
        IsDownloading = true;
        OnStatusChanged?.Invoke("Downloading yt-dlp...");

        try
        {
            var downloadUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
            var targetPath = _configService.YtDlpPath;

            await DownloadFileAsync(downloadUrl, targetPath, cancellationToken);

            // Update config with version
            if (!string.IsNullOrEmpty(LatestYtDlpVersion))
            {
                _configService.UpdateConfig(c => c.YtDlpVersion = LatestYtDlpVersion);
            }

            YtDlpUpdateAvailable = false;
            OnStatusChanged?.Invoke("yt-dlp downloaded successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download yt-dlp");
            OnStatusChanged?.Invoke($"Error: {ex.Message}");
            throw;
        }
        finally
        {
            IsDownloading = false;
        }
    }

    public async Task DownloadFfmpegAsync(CancellationToken cancellationToken = default)
    {
        IsDownloading = true;
        OnStatusChanged?.Invoke("Downloading FFmpeg...");

        try
        {
            // Using gyan.dev essentials build
            var downloadUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
            var tempZipPath = Path.Combine(Path.GetTempPath(), "ffmpeg.zip");
            var tempExtractPath = Path.Combine(Path.GetTempPath(), "ffmpeg_extract");

            await DownloadFileAsync(downloadUrl, tempZipPath, cancellationToken);

            OnStatusChanged?.Invoke("Extracting FFmpeg...");
            OnProgressChanged?.Invoke(0);

            // Clean up extract directory
            if (Directory.Exists(tempExtractPath))
                Directory.Delete(tempExtractPath, true);

            ZipFile.ExtractToDirectory(tempZipPath, tempExtractPath);

            // Find ffmpeg.exe in the extracted folder
            var ffmpegExe = Directory.GetFiles(tempExtractPath, "ffmpeg.exe", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (ffmpegExe == null)
                throw new Exception("ffmpeg.exe not found in downloaded archive");

            // Copy to tools directory
            File.Copy(ffmpegExe, _configService.FfmpegPath, overwrite: true);

            // Clean up
            try
            {
                File.Delete(tempZipPath);
                Directory.Delete(tempExtractPath, true);
            }
            catch { }

            // Update config
            if (!string.IsNullOrEmpty(LatestFfmpegVersion))
            {
                _configService.UpdateConfig(c => c.FfmpegVersion = LatestFfmpegVersion);
            }

            FfmpegUpdateAvailable = false;
            OnStatusChanged?.Invoke("FFmpeg downloaded successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download FFmpeg");
            OnStatusChanged?.Invoke($"Error: {ex.Message}");
            throw;
        }
        finally
        {
            IsDownloading = false;
        }
    }

    private async Task DownloadFileAsync(string url, string targetPath, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var downloadedBytes = 0L;

        // Ensure directory exists
        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            downloadedBytes += bytesRead;

            if (totalBytes > 0)
            {
                var progress = (double)downloadedBytes / totalBytes * 100;
                OnProgressChanged?.Invoke(progress);
            }
        }

        OnProgressChanged?.Invoke(100);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

public class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("published_at")]
    public DateTime? PublishedAt { get; set; }

    [JsonPropertyName("assets")]
    public List<GitHubAsset>? Assets { get; set; }
}

public class GitHubAsset
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }
}
