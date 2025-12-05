using MediaGrabber.Models;

namespace MediaGrabber.Services;

public class PresetService
{
    private readonly ConfigService _configService;

    public PresetService(ConfigService configService)
    {
        _configService = configService;
    }

    public Dictionary<string, CreatorPreset> BuiltInPresets { get; } = new()
    {
        ["youtube_upload"] = new CreatorPreset
        {
            Id = "youtube_upload",
            Name = "YouTube Upload",
            Description = "Optimal settings for YouTube uploads (H.264, AAC, 1080p max)",
            Icon = "bi-youtube",
            Options = new FFmpegOptions
            {
                VideoCodec = "libx264",
                AudioCodec = "aac",
                Container = "mp4",
                Crf = 18,
                Preset = "slow",
                AudioBitrate = "192k",
                MaxHeight = 1080
            }
        },
        ["discord_8mb"] = new CreatorPreset
        {
            Id = "discord_8mb",
            Name = "Discord (8MB)",
            Description = "Compressed for Discord free tier (8MB limit)",
            Icon = "bi-discord",
            Options = new FFmpegOptions
            {
                VideoCodec = "libx264",
                AudioCodec = "aac",
                Container = "mp4",
                MaxFileSizeMB = 8,
                Preset = "medium",
                AudioBitrate = "96k",
                TwoPass = true
            }
        },
        ["discord_25mb"] = new CreatorPreset
        {
            Id = "discord_25mb",
            Name = "Discord (25MB)",
            Description = "Compressed for Discord Nitro Basic (25MB limit)",
            Icon = "bi-discord",
            Options = new FFmpegOptions
            {
                VideoCodec = "libx264",
                AudioCodec = "aac",
                Container = "mp4",
                MaxFileSizeMB = 25,
                Preset = "medium",
                AudioBitrate = "128k",
                TwoPass = true
            }
        },
        ["discord_50mb"] = new CreatorPreset
        {
            Id = "discord_50mb",
            Name = "Discord (50MB)",
            Description = "Compressed for Discord Nitro (50MB limit)",
            Icon = "bi-discord",
            Options = new FFmpegOptions
            {
                VideoCodec = "libx264",
                AudioCodec = "aac",
                Container = "mp4",
                MaxFileSizeMB = 50,
                Preset = "slow",
                AudioBitrate = "160k",
                TwoPass = true
            }
        },
        ["twitter"] = new CreatorPreset
        {
            Id = "twitter",
            Name = "Twitter/X",
            Description = "Optimized for Twitter (MP4, max 2:20, <512MB)",
            Icon = "bi-twitter-x",
            Options = new FFmpegOptions
            {
                VideoCodec = "libx264",
                AudioCodec = "aac",
                Container = "mp4",
                Crf = 23,
                Preset = "medium",
                AudioBitrate = "128k",
                MaxFileSizeMB = 512,
                Duration = "140" // 2:20 max
            }
        },
        ["tiktok"] = new CreatorPreset
        {
            Id = "tiktok",
            Name = "TikTok",
            Description = "Vertical video for TikTok (9:16, max 60s)",
            Icon = "bi-tiktok",
            Options = new FFmpegOptions
            {
                VideoCodec = "libx264",
                AudioCodec = "aac",
                Container = "mp4",
                Crf = 20,
                Preset = "medium",
                AspectRatio = "9:16",
                AudioBitrate = "128k",
                Duration = "60"
            }
        },
        ["instagram_reels"] = new CreatorPreset
        {
            Id = "instagram_reels",
            Name = "Instagram Reels",
            Description = "Vertical video for Instagram Reels (9:16, max 90s)",
            Icon = "bi-instagram",
            Options = new FFmpegOptions
            {
                VideoCodec = "libx264",
                AudioCodec = "aac",
                Container = "mp4",
                Crf = 20,
                Preset = "medium",
                AspectRatio = "9:16",
                MaxHeight = 1920,
                AudioBitrate = "128k",
                Duration = "90"
            }
        },
        ["podcast_audio"] = new CreatorPreset
        {
            Id = "podcast_audio",
            Name = "Audio Podcast",
            Description = "MP3 192kbps mono with loudness normalization",
            Icon = "bi-mic",
            Options = new FFmpegOptions
            {
                AudioCodec = "libmp3lame",
                Container = "mp3",
                AudioBitrate = "192k",
                AudioChannels = 1,
                AudioSampleRate = 44100,
                NormalizeAudio = true
            }
        },
        ["audiobook"] = new CreatorPreset
        {
            Id = "audiobook",
            Name = "Audiobook",
            Description = "M4A 64kbps mono optimized for speech",
            Icon = "bi-book",
            Options = new FFmpegOptions
            {
                AudioCodec = "aac",
                Container = "m4a",
                AudioBitrate = "64k",
                AudioChannels = 1,
                AudioSampleRate = 44100,
                NormalizeAudio = true
            }
        },
        ["high_quality_archive"] = new CreatorPreset
        {
            Id = "high_quality_archive",
            Name = "High Quality Archive",
            Description = "Maximum quality for archival (H.265, OPUS, MKV)",
            Icon = "bi-archive",
            Options = new FFmpegOptions
            {
                VideoCodec = "libx265",
                AudioCodec = "libopus",
                Container = "mkv",
                Crf = 18,
                Preset = "slow",
                AudioBitrate = "192k"
            }
        },
        ["gif_conversion"] = new CreatorPreset
        {
            Id = "gif_conversion",
            Name = "GIF (10s max)",
            Description = "Convert to GIF, max 10 seconds, 480p",
            Icon = "bi-filetype-gif",
            Options = new FFmpegOptions
            {
                Container = "gif",
                MaxWidth = 480,
                Fps = 15,
                Duration = "10"
            }
        }
    };

    public Dictionary<string, FFmpegPreset> CustomPresets => _configService.Config.CustomPresets;

    public IEnumerable<CreatorPreset> GetAllPresets()
    {
        foreach (var preset in BuiltInPresets.Values)
        {
            yield return preset;
        }

        foreach (var (id, preset) in CustomPresets)
        {
            yield return new CreatorPreset
            {
                Id = id,
                Name = preset.Name,
                Description = preset.Description,
                Icon = "bi-gear",
                Options = preset.Options,
                IsCustom = true
            };
        }
    }

    public CreatorPreset? GetPreset(string id)
    {
        if (BuiltInPresets.TryGetValue(id, out var builtin))
            return builtin;

        if (CustomPresets.TryGetValue(id, out var custom))
        {
            return new CreatorPreset
            {
                Id = id,
                Name = custom.Name,
                Description = custom.Description,
                Icon = "bi-gear",
                Options = custom.Options,
                IsCustom = true
            };
        }

        return null;
    }

    public void SaveCustomPreset(string id, string name, string description, FFmpegOptions options)
    {
        _configService.UpdateConfig(c =>
        {
            c.CustomPresets[id] = new FFmpegPreset
            {
                Name = name,
                Description = description,
                Options = options
            };
        });
    }

    public void DeleteCustomPreset(string id)
    {
        _configService.UpdateConfig(c => c.CustomPresets.Remove(id));
    }
}

public class CreatorPreset
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "bi-gear";
    public FFmpegOptions Options { get; set; } = new();
    public bool IsCustom { get; set; }
}
