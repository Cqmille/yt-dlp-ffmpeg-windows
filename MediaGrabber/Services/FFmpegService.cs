using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using MediaGrabber.Models;

namespace MediaGrabber.Services;

public partial class FFmpegService
{
    private readonly ConfigService _configService;
    private readonly ILogger<FFmpegService> _logger;

    public FFmpegService(ConfigService configService, ILogger<FFmpegService> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public bool IsInstalled => _configService.IsFfmpegInstalled();

    public async Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInstalled)
            return "Not installed";

        var (exitCode, _, error) = await RunAsync(["-version"], cancellationToken: cancellationToken);
        if (exitCode != 0)
            return "Unknown";

        // FFmpeg outputs version to stderr
        var match = VersionRegex().Match(error);
        return match.Success ? match.Groups[1].Value : "Unknown";
    }

    public async Task<TimeSpan?> GetDurationAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!IsInstalled || !File.Exists(filePath))
            return null;

        var (exitCode, _, error) = await RunAsync(
            ["-i", filePath, "-f", "null", "-"],
            cancellationToken: cancellationToken);

        var match = DurationRegex().Match(error);
        if (match.Success && TimeSpan.TryParse(match.Groups[1].Value, out var duration))
        {
            return duration;
        }

        return null;
    }

    public async Task ProcessAsync(
        string inputPath,
        string outputPath,
        FFmpegOptions options,
        Action<double, TimeSpan?>? onProgress = null,
        Action<string>? onLog = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsInstalled)
            throw new InvalidOperationException("FFmpeg is not installed");

        // Get input duration for progress calculation
        var duration = await GetDurationAsync(inputPath, cancellationToken);

        var arguments = BuildArguments(inputPath, outputPath, options);
        onLog?.Invoke($"FFmpeg command: ffmpeg {string.Join(" ", arguments)}");

        await RunAsync(
            arguments,
            onError: line =>
            {
                onLog?.Invoke(line);

                // Parse progress
                var timeMatch = TimeRegex().Match(line);
                if (timeMatch.Success && TimeSpan.TryParse(timeMatch.Groups[1].Value, out var currentTime))
                {
                    if (duration.HasValue && duration.Value.TotalSeconds > 0)
                    {
                        var progress = (currentTime.TotalSeconds / duration.Value.TotalSeconds) * 100;
                        onProgress?.Invoke(Math.Min(progress, 100), currentTime);
                    }
                }
            },
            cancellationToken: cancellationToken);
    }

    public List<string> BuildArguments(string inputPath, string outputPath, FFmpegOptions options)
    {
        var args = new List<string>();

        // Always overwrite
        args.Add("-y");

        // Hardware acceleration
        if (!string.IsNullOrEmpty(options.HwAccel))
        {
            args.AddRange(["-hwaccel", options.HwAccel]);
        }

        // Extra input args
        if (!string.IsNullOrEmpty(options.ExtraInputArgs))
        {
            args.AddRange(options.ExtraInputArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        // Start time (before input for fast seeking)
        if (!string.IsNullOrEmpty(options.StartTime))
        {
            args.AddRange(["-ss", options.StartTime]);
        }

        // Input file
        args.AddRange(["-i", inputPath]);

        // End time / duration (after input)
        if (!string.IsNullOrEmpty(options.EndTime))
        {
            args.AddRange(["-to", options.EndTime]);
        }
        else if (!string.IsNullOrEmpty(options.Duration))
        {
            args.AddRange(["-t", options.Duration]);
        }

        // Video codec
        if (!string.IsNullOrEmpty(options.VideoCodec))
        {
            args.AddRange(["-c:v", options.VideoCodec]);

            if (options.VideoCodec != "copy")
            {
                // CRF or bitrate
                if (options.Crf.HasValue)
                {
                    args.AddRange(["-crf", options.Crf.Value.ToString()]);
                }
                else if (!string.IsNullOrEmpty(options.VideoBitrate))
                {
                    args.AddRange(["-b:v", options.VideoBitrate]);
                }

                // Preset
                if (!string.IsNullOrEmpty(options.Preset))
                {
                    args.AddRange(["-preset", options.Preset]);
                }
            }
        }

        // Resolution / scaling
        var videoFilters = new List<string>();

        if (!string.IsNullOrEmpty(options.Resolution))
        {
            videoFilters.Add($"scale={options.Resolution.Replace("x", ":")}");
        }
        else if (options.MaxWidth.HasValue || options.MaxHeight.HasValue)
        {
            var w = options.MaxWidth?.ToString() ?? "-1";
            var h = options.MaxHeight?.ToString() ?? "-1";
            videoFilters.Add($"scale='min({w},iw)':min'({h},ih)':force_original_aspect_ratio=decrease");
        }

        // FPS
        if (options.Fps.HasValue)
        {
            videoFilters.Add($"fps={options.Fps.Value}");
        }

        // Aspect ratio padding
        if (!string.IsNullOrEmpty(options.AspectRatio))
        {
            var (padW, padH) = ParseAspectRatio(options.AspectRatio);
            videoFilters.Add($"pad={padW}:{padH}:(ow-iw)/2:(oh-ih)/2");
        }

        if (videoFilters.Count > 0)
        {
            args.AddRange(["-vf", string.Join(",", videoFilters)]);
        }

        // Audio codec
        if (!string.IsNullOrEmpty(options.AudioCodec))
        {
            args.AddRange(["-c:a", options.AudioCodec]);

            if (options.AudioCodec != "copy")
            {
                if (!string.IsNullOrEmpty(options.AudioBitrate))
                {
                    args.AddRange(["-b:a", options.AudioBitrate]);
                }

                if (options.AudioSampleRate.HasValue)
                {
                    args.AddRange(["-ar", options.AudioSampleRate.Value.ToString()]);
                }

                if (options.AudioChannels.HasValue)
                {
                    args.AddRange(["-ac", options.AudioChannels.Value.ToString()]);
                }
            }
        }

        // Audio normalization
        if (options.NormalizeAudio)
        {
            args.AddRange(["-af", "loudnorm=I=-16:TP=-1.5:LRA=11"]);
        }

        // Extra output args
        if (!string.IsNullOrEmpty(options.ExtraOutputArgs))
        {
            args.AddRange(options.ExtraOutputArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        // Output file
        args.Add(outputPath);

        return args;
    }

    public string GenerateCommandPreview(string inputPath, string outputPath, FFmpegOptions options)
    {
        var args = BuildArguments(inputPath, outputPath, options);
        return $"ffmpeg {string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))}";
    }

    private static (string w, string h) ParseAspectRatio(string ratio)
    {
        return ratio switch
        {
            "16:9" => ("ceil(ih*16/9/2)*2", "ih"),
            "9:16" => ("iw", "ceil(iw*16/9/2)*2"),
            "4:3" => ("ceil(ih*4/3/2)*2", "ih"),
            "1:1" => ("max(iw,ih)", "max(iw,ih)"),
            _ => ("iw", "ih")
        };
    }

    private async Task<(int ExitCode, string Output, string Error)> RunAsync(
        List<string> arguments,
        Action<string>? onOutput = null,
        Action<string>? onError = null,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _configService.FfmpegPath,
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

    [GeneratedRegex(@"ffmpeg version (\S+)")]
    private static partial Regex VersionRegex();

    [GeneratedRegex(@"Duration: (\d{2}:\d{2}:\d{2}\.\d{2})")]
    private static partial Regex DurationRegex();

    [GeneratedRegex(@"time=(\d{2}:\d{2}:\d{2}\.\d{2})")]
    private static partial Regex TimeRegex();
}
