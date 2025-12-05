using System.Text.Json.Serialization;

namespace MediaGrabber.Models;

public class FFmpegOptions
{
    // Video options
    [JsonPropertyName("videoCodec")]
    public string? VideoCodec { get; set; } // libx264, libx265, libvpx-vp9, libaom-av1

    [JsonPropertyName("videoBitrate")]
    public string? VideoBitrate { get; set; } // e.g., "5M", "2500k"

    [JsonPropertyName("crf")]
    public int? Crf { get; set; } // 0-51 for x264/x265

    [JsonPropertyName("preset")]
    public string? Preset { get; set; } // ultrafast, superfast, veryfast, faster, fast, medium, slow, slower, veryslow

    [JsonPropertyName("resolution")]
    public string? Resolution { get; set; } // e.g., "1920x1080", "1280x720"

    [JsonPropertyName("maxWidth")]
    public int? MaxWidth { get; set; }

    [JsonPropertyName("maxHeight")]
    public int? MaxHeight { get; set; }

    [JsonPropertyName("fps")]
    public int? Fps { get; set; }

    [JsonPropertyName("aspectRatio")]
    public string? AspectRatio { get; set; } // e.g., "16:9", "9:16"

    // Audio options
    [JsonPropertyName("audioCodec")]
    public string? AudioCodec { get; set; } // aac, libopus, libmp3lame

    [JsonPropertyName("audioBitrate")]
    public string? AudioBitrate { get; set; } // e.g., "192k", "320k"

    [JsonPropertyName("audioSampleRate")]
    public int? AudioSampleRate { get; set; } // 44100, 48000

    [JsonPropertyName("audioChannels")]
    public int? AudioChannels { get; set; } // 1 (mono), 2 (stereo)

    [JsonPropertyName("normalizeAudio")]
    public bool NormalizeAudio { get; set; } // loudnorm filter

    // Trimming
    [JsonPropertyName("startTime")]
    public string? StartTime { get; set; } // e.g., "00:01:30" or "90"

    [JsonPropertyName("endTime")]
    public string? EndTime { get; set; }

    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

    // Container
    [JsonPropertyName("container")]
    public string? Container { get; set; } // mp4, mkv, webm, mp3

    // Size limits (for platform-specific encoding)
    [JsonPropertyName("maxFileSizeMB")]
    public int? MaxFileSizeMB { get; set; }

    // Two-pass encoding
    [JsonPropertyName("twoPass")]
    public bool TwoPass { get; set; }

    // Hardware acceleration
    [JsonPropertyName("hwAccel")]
    public string? HwAccel { get; set; } // cuda, qsv, d3d11va

    // Custom options
    [JsonPropertyName("extraInputArgs")]
    public string? ExtraInputArgs { get; set; }

    [JsonPropertyName("extraOutputArgs")]
    public string? ExtraOutputArgs { get; set; }

    public FFmpegOptions Clone()
    {
        return new FFmpegOptions
        {
            VideoCodec = VideoCodec,
            VideoBitrate = VideoBitrate,
            Crf = Crf,
            Preset = Preset,
            Resolution = Resolution,
            MaxWidth = MaxWidth,
            MaxHeight = MaxHeight,
            Fps = Fps,
            AspectRatio = AspectRatio,
            AudioCodec = AudioCodec,
            AudioBitrate = AudioBitrate,
            AudioSampleRate = AudioSampleRate,
            AudioChannels = AudioChannels,
            NormalizeAudio = NormalizeAudio,
            StartTime = StartTime,
            EndTime = EndTime,
            Duration = Duration,
            Container = Container,
            MaxFileSizeMB = MaxFileSizeMB,
            TwoPass = TwoPass,
            HwAccel = HwAccel,
            ExtraInputArgs = ExtraInputArgs,
            ExtraOutputArgs = ExtraOutputArgs
        };
    }
}

public static class FFmpegCodecs
{
    public static readonly Dictionary<string, string> VideoCodecs = new()
    {
        { "libx264", "H.264 (x264)" },
        { "libx265", "H.265/HEVC (x265)" },
        { "libvpx-vp9", "VP9" },
        { "libaom-av1", "AV1" },
        { "copy", "Copy (no re-encode)" }
    };

    public static readonly Dictionary<string, string> AudioCodecs = new()
    {
        { "aac", "AAC" },
        { "libopus", "Opus" },
        { "libmp3lame", "MP3" },
        { "copy", "Copy (no re-encode)" }
    };

    public static readonly Dictionary<string, string> Presets = new()
    {
        { "ultrafast", "Ultrafast (lowest quality)" },
        { "superfast", "Superfast" },
        { "veryfast", "Very Fast" },
        { "faster", "Faster" },
        { "fast", "Fast" },
        { "medium", "Medium (default)" },
        { "slow", "Slow (better quality)" },
        { "slower", "Slower" },
        { "veryslow", "Very Slow (best quality)" }
    };

    public static readonly Dictionary<string, string> Containers = new()
    {
        { "mp4", "MP4" },
        { "mkv", "MKV (Matroska)" },
        { "webm", "WebM" },
        { "mov", "MOV (QuickTime)" },
        { "mp3", "MP3 (Audio)" },
        { "m4a", "M4A (Audio)" },
        { "opus", "Opus (Audio)" }
    };

    public static readonly string[] CommonResolutions =
    [
        "3840x2160",
        "2560x1440",
        "1920x1080",
        "1280x720",
        "854x480",
        "640x360"
    ];
}
