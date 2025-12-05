# MediaGrabber

A Blazor Server application for downloading and processing media via yt-dlp and FFmpeg.

## Features

- **Video Downloads**: Download videos from YouTube, Twitch, Twitter/X, TikTok, and 1000+ other sites
- **Format Selection**: Choose from available formats with detailed codec, resolution, and bitrate info
- **Post-Processing**: Powerful FFmpeg integration with creator presets for YouTube, Discord, Twitter, TikTok
- **Queue Management**: Background download queue with real-time progress and logs
- **Auto-Updates**: Automatic detection and download of yt-dlp and FFmpeg updates

## Quick Start

### Prerequisites

- Windows 10/11 (x64)
- No additional runtime required (self-contained)

### Running

1. Double-click `MediaGrabber.exe`
2. Open your browser to `http://localhost:5000`
3. On first launch, go to **Settings** and click **Download** for both yt-dlp and FFmpeg

## Building from Source

### Requirements

- .NET 8.0 SDK

### Build Commands

```bash
# Development
cd MediaGrabber
dotnet run

# Production (single-file executable)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The output will be in `MediaGrabber/bin/Release/net8.0/win-x64/publish/`

## Creator Presets

Built-in presets optimized for popular platforms:

| Preset | Description |
|--------|-------------|
| YouTube Upload | H.264, AAC, MP4, optimal quality |
| Discord (8/25/50MB) | Compressed to fit Discord limits |
| Twitter/X | MP4, max 2:20, <512MB |
| TikTok | 9:16 vertical, max 60s |
| Instagram Reels | 9:16 vertical, max 90s |
| Audio Podcast | MP3 192kbps mono, normalized |
| High Quality Archive | H.265, Opus, MKV |

## Configuration

Settings are stored in `config.json` next to the executable:

```json
{
  "outputDirectory": "C:\\Users\\...\\Downloads\\MediaGrabber",
  "defaultVideoFormat": "bestvideo+bestaudio/best",
  "embedSubtitles": true,
  "embedMetadata": true,
  "maxConcurrentDownloads": 2
}
```

## Project Structure

```
MediaGrabber/
├── Components/
│   ├── Layout/MainLayout.razor
│   └── Pages/
│       ├── Home.razor          # Dashboard
│       ├── Download.razor      # URL input & format selection
│       ├── PostProcess.razor   # FFmpeg processing
│       ├── Queue.razor         # Download queue
│       └── Settings.razor      # Configuration
├── Services/
│   ├── YtDlpService.cs        # yt-dlp wrapper
│   ├── FFmpegService.cs       # FFmpeg wrapper
│   ├── QueueService.cs        # Background job processing
│   ├── UpdateService.cs       # Binary updates
│   ├── PresetService.cs       # Creator presets
│   └── ConfigService.cs       # Configuration management
├── Models/
│   ├── VideoMetadata.cs       # Video info from yt-dlp
│   ├── FormatInfo.cs          # Available format details
│   ├── DownloadJob.cs         # Queue job model
│   ├── FFmpegOptions.cs       # Processing options
│   └── AppConfig.cs           # App configuration
└── Tools/                     # yt-dlp.exe & ffmpeg.exe
```

## Keyboard Shortcuts

- `Ctrl+V` in URL field: Paste and auto-fetch

## License

MIT

## Credits

- [yt-dlp](https://github.com/yt-dlp/yt-dlp) - Video downloader
- [FFmpeg](https://ffmpeg.org/) - Media processing
- [Bootstrap](https://getbootstrap.com/) - UI framework
- [Bootstrap Icons](https://icons.getbootstrap.com/) - Icon set
