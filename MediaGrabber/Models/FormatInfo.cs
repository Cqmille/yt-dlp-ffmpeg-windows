using System.Text.Json.Serialization;

namespace MediaGrabber.Models;

public class FormatInfo
{
    [JsonPropertyName("format_id")]
    public string FormatId { get; set; } = "";

    [JsonPropertyName("format_note")]
    public string? FormatNote { get; set; }

    [JsonPropertyName("ext")]
    public string? Extension { get; set; }

    [JsonPropertyName("resolution")]
    public string? Resolution { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("fps")]
    public double? Fps { get; set; }

    [JsonPropertyName("vcodec")]
    public string? VideoCodec { get; set; }

    [JsonPropertyName("acodec")]
    public string? AudioCodec { get; set; }

    [JsonPropertyName("vbr")]
    public double? VideoBitrate { get; set; }

    [JsonPropertyName("abr")]
    public double? AudioBitrate { get; set; }

    [JsonPropertyName("tbr")]
    public double? TotalBitrate { get; set; }

    [JsonPropertyName("filesize")]
    public long? FileSize { get; set; }

    [JsonPropertyName("filesize_approx")]
    public long? FileSizeApprox { get; set; }

    [JsonPropertyName("asr")]
    public int? AudioSampleRate { get; set; }

    [JsonPropertyName("audio_channels")]
    public int? AudioChannels { get; set; }

    [JsonPropertyName("protocol")]
    public string? Protocol { get; set; }

    [JsonPropertyName("container")]
    public string? Container { get; set; }

    [JsonPropertyName("dynamic_range")]
    public string? DynamicRange { get; set; }

    public bool HasVideo => VideoCodec != null && VideoCodec != "none";
    public bool HasAudio => AudioCodec != null && AudioCodec != "none";
    public bool IsVideoOnly => HasVideo && !HasAudio;
    public bool IsAudioOnly => HasAudio && !HasVideo;

    public string DisplayResolution
    {
        get
        {
            if (Width.HasValue && Height.HasValue)
                return $"{Width}x{Height}";
            if (!string.IsNullOrEmpty(Resolution) && Resolution != "audio only")
                return Resolution;
            return IsAudioOnly ? "Audio" : "Unknown";
        }
    }

    public string DisplayFps => Fps.HasValue ? $"{Fps:F0}" : "-";

    public string DisplayVideoCodec
    {
        get
        {
            if (!HasVideo) return "-";
            var codec = VideoCodec ?? "";
            // Simplify codec names
            if (codec.StartsWith("avc1") || codec.StartsWith("h264")) return "H.264";
            if (codec.StartsWith("hvc1") || codec.StartsWith("hevc") || codec.StartsWith("h265")) return "H.265";
            if (codec.StartsWith("vp9") || codec.StartsWith("vp09")) return "VP9";
            if (codec.StartsWith("av01") || codec.StartsWith("av1")) return "AV1";
            return codec.Length > 10 ? codec[..10] : codec;
        }
    }

    public string DisplayAudioCodec
    {
        get
        {
            if (!HasAudio) return "-";
            var codec = AudioCodec ?? "";
            if (codec.StartsWith("mp4a") || codec.Contains("aac")) return "AAC";
            if (codec.StartsWith("opus")) return "Opus";
            if (codec.Contains("mp3")) return "MP3";
            if (codec.StartsWith("vorbis")) return "Vorbis";
            return codec.Length > 10 ? codec[..10] : codec;
        }
    }

    public string DisplayFileSize
    {
        get
        {
            var size = FileSize ?? FileSizeApprox;
            if (!size.HasValue) return "Unknown";
            return FormatBytes(size.Value);
        }
    }

    public string FormatType
    {
        get
        {
            if (IsVideoOnly) return "Video Only";
            if (IsAudioOnly) return "Audio Only";
            if (HasVideo && HasAudio) return "Video+Audio";
            return "Unknown";
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
