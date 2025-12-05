using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MediaGrabber.Models;

namespace MediaGrabber.Services;

public partial class YtDlpService
{
    private readonly ConfigService _configService;
    private readonly ILogger<YtDlpService> _logger;

    public YtDlpService(ConfigService configService, ILogger<YtDlpService> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public bool IsInstalled => _configService.IsYtDlpInstalled();

    public async Task<VideoMetadata?> GetMetadataAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!IsInstalled)
            throw new InvalidOperationException("yt-dlp is not installed");

        var arguments = new List<string>
        {
            "--dump-json",
            "--no-playlist",
            "--no-warnings",
            "--encoding", "utf-8",
            url
        };

        var (exitCode, output, error) = await RunAsync(arguments, cancellationToken: cancellationToken);

        if (exitCode != 0)
        {
            _logger.LogError("yt-dlp metadata fetch failed: {Error}", error);
            throw new Exception($"Failed to fetch metadata: {error}");
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<VideoMetadata>(output, options);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse yt-dlp JSON output");
            throw new Exception("Failed to parse video metadata");
        }
    }

    public async Task DownloadAsync(
        DownloadJob job,
        Action<double, string?, string?>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsInstalled)
            throw new InvalidOperationException("yt-dlp is not installed");

        var outputTemplate = Path.Combine(
            job.OutputDirectory,
            "%(title)s.%(ext)s"
        );

        var arguments = new List<string>
        {
            "--no-playlist",
            "--no-warnings",
            "--encoding", "utf-8",
            "--newline",
            "--progress",
            "-f", job.FormatSelector,
            "-o", outputTemplate
        };

        if (job.EmbedSubtitles)
        {
            arguments.AddRange(["--embed-subs", "--sub-langs", "all"]);
        }

        if (job.EmbedMetadata)
        {
            arguments.Add("--embed-metadata");
        }

        if (job.ExtractAudio && !string.IsNullOrEmpty(job.AudioFormat))
        {
            arguments.AddRange(["-x", "--audio-format", job.AudioFormat]);
        }

        // Add ffmpeg location if available
        if (_configService.IsFfmpegInstalled())
        {
            arguments.AddRange(["--ffmpeg-location", _configService.ToolsDirectory]);
        }

        arguments.Add(job.Url);

        var outputFile = string.Empty;

        await RunAsync(
            arguments,
            onOutput: line =>
            {
                // Parse progress
                var progressMatch = ProgressRegex().Match(line);
                if (progressMatch.Success)
                {
                    var percentStr = progressMatch.Groups[1].Value;
                    if (double.TryParse(percentStr, out var percent))
                    {
                        var speed = progressMatch.Groups[2].Success ? progressMatch.Groups[2].Value : null;
                        var eta = progressMatch.Groups[3].Success ? progressMatch.Groups[3].Value : null;
                        onProgress?.Invoke(percent, speed, eta);
                    }
                }

                // Parse destination filename
                var destMatch = DestinationRegex().Match(line);
                if (destMatch.Success)
                {
                    outputFile = destMatch.Groups[1].Value;
                }

                // Also check merger output
                var mergerMatch = MergerRegex().Match(line);
                if (mergerMatch.Success)
                {
                    outputFile = mergerMatch.Groups[1].Value;
                }

                job.AddLog(line, LogLevel.Debug);
            },
            onError: line =>
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    job.AddLog(line, LogLevel.Warning);
                }
            },
            cancellationToken: cancellationToken
        );

        job.OutputPath = outputFile;
    }

    public async Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInstalled)
            return "Not installed";

        var (exitCode, output, _) = await RunAsync(["--version"], cancellationToken: cancellationToken);
        return exitCode == 0 ? output.Trim() : "Unknown";
    }

    public async Task<List<string>> GetSupportedSitesAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInstalled)
            return [];

        var (exitCode, output, _) = await RunAsync(["--list-extractors"], cancellationToken: cancellationToken);
        if (exitCode != 0)
            return [];

        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }

    private async Task<(int ExitCode, string Output, string Error)> RunAsync(
        List<string> arguments,
        Action<string>? onOutput = null,
        Action<string>? onError = null,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _configService.YtDlpPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = psi };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                onOutput?.Invoke(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
                onError?.Invoke(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch { }
            throw;
        }

        return (process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
    }

    [GeneratedRegex(@"\[download\]\s+(\d+\.?\d*)%.*?(?:at\s+(\S+))?.*?(?:ETA\s+(\S+))?")]
    private static partial Regex ProgressRegex();

    [GeneratedRegex(@"\[download\] Destination: (.+)")]
    private static partial Regex DestinationRegex();

    [GeneratedRegex(@"\[Merger\] Merging formats into ""(.+)""")]
    private static partial Regex MergerRegex();
}
