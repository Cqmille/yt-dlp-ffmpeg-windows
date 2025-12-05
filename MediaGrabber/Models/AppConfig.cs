using System.Text.Json.Serialization;

namespace MediaGrabber.Models;

public class AppConfig
{
    [JsonPropertyName("outputDirectory")]
    public string OutputDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Downloads", "MediaGrabber");

    [JsonPropertyName("ytDlpVersion")]
    public string? YtDlpVersion { get; set; }

    [JsonPropertyName("ffmpegVersion")]
    public string? FfmpegVersion { get; set; }

    [JsonPropertyName("defaultVideoFormat")]
    public string DefaultVideoFormat { get; set; } = "bestvideo+bestaudio/best";

    [JsonPropertyName("embedSubtitles")]
    public bool EmbedSubtitles { get; set; } = true;

    [JsonPropertyName("embedMetadata")]
    public bool EmbedMetadata { get; set; } = true;

    [JsonPropertyName("enableNotificationSound")]
    public bool EnableNotificationSound { get; set; } = true;

    [JsonPropertyName("maxConcurrentDownloads")]
    public int MaxConcurrentDownloads { get; set; } = 2;

    [JsonPropertyName("downloadTimeout")]
    public int DownloadTimeoutMinutes { get; set; } = 60;

    [JsonPropertyName("customPresets")]
    public Dictionary<string, FFmpegPreset> CustomPresets { get; set; } = new();
}

public class FFmpegPreset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("options")]
    public FFmpegOptions Options { get; set; } = new();
}
