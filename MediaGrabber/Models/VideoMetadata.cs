using System.Text.Json.Serialization;

namespace MediaGrabber.Models;

public class VideoMetadata
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("duration")]
    public double? Duration { get; set; }

    [JsonPropertyName("duration_string")]
    public string? DurationString { get; set; }

    [JsonPropertyName("uploader")]
    public string? Uploader { get; set; }

    [JsonPropertyName("uploader_id")]
    public string? UploaderId { get; set; }

    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    [JsonPropertyName("upload_date")]
    public string? UploadDate { get; set; }

    [JsonPropertyName("view_count")]
    public long? ViewCount { get; set; }

    [JsonPropertyName("like_count")]
    public long? LikeCount { get; set; }

    [JsonPropertyName("webpage_url")]
    public string? WebpageUrl { get; set; }

    [JsonPropertyName("extractor")]
    public string? Extractor { get; set; }

    [JsonPropertyName("extractor_key")]
    public string? ExtractorKey { get; set; }

    [JsonPropertyName("formats")]
    public List<FormatInfo> Formats { get; set; } = new();

    [JsonPropertyName("subtitles")]
    public Dictionary<string, List<SubtitleInfo>>? Subtitles { get; set; }

    [JsonPropertyName("automatic_captions")]
    public Dictionary<string, List<SubtitleInfo>>? AutomaticCaptions { get; set; }

    [JsonPropertyName("is_live")]
    public bool? IsLive { get; set; }

    [JsonPropertyName("was_live")]
    public bool? WasLive { get; set; }

    public string FormattedDuration => Duration.HasValue
        ? TimeSpan.FromSeconds(Duration.Value).ToString(@"hh\:mm\:ss")
        : DurationString ?? "Unknown";

    public string FormattedUploadDate
    {
        get
        {
            if (string.IsNullOrEmpty(UploadDate) || UploadDate.Length != 8)
                return UploadDate ?? "Unknown";

            if (DateTime.TryParseExact(UploadDate, "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.None, out var date))
            {
                return date.ToString("yyyy-MM-dd");
            }
            return UploadDate;
        }
    }

    public string FormattedViewCount => ViewCount.HasValue
        ? ViewCount.Value.ToString("N0")
        : "Unknown";

    public string PlatformName => ExtractorKey?.ToLower() switch
    {
        "youtube" => "YouTube",
        "twitch" => "Twitch",
        "twitchclips" => "Twitch Clip",
        "twitchvod" => "Twitch VOD",
        "twitter" => "Twitter/X",
        "tiktok" => "TikTok",
        "instagram" => "Instagram",
        "facebook" => "Facebook",
        "vimeo" => "Vimeo",
        "dailymotion" => "Dailymotion",
        "reddit" => "Reddit",
        _ => ExtractorKey ?? "Unknown"
    };
}

public class SubtitleInfo
{
    [JsonPropertyName("ext")]
    public string? Extension { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
