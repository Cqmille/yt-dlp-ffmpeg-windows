using System.Text.Json;
using MediaGrabber.Models;

namespace MediaGrabber.Services;

public class ConfigService
{
    private readonly string _configPath;
    private readonly string _appDirectory;
    private AppConfig _config = new();
    private readonly object _lock = new();

    public event Action? OnConfigChanged;

    public ConfigService()
    {
        _appDirectory = AppContext.BaseDirectory;
        _configPath = Path.Combine(_appDirectory, "config.json");
        LoadConfig();
        EnsureDirectories();
    }

    public AppConfig Config
    {
        get
        {
            lock (_lock)
            {
                return _config;
            }
        }
    }

    public string AppDirectory => _appDirectory;
    public string ToolsDirectory => Path.Combine(_appDirectory, "Tools");
    public string YtDlpPath => Path.Combine(ToolsDirectory, "yt-dlp.exe");
    public string FfmpegPath => Path.Combine(ToolsDirectory, "ffmpeg.exe");

    public void UpdateConfig(Action<AppConfig> updateAction)
    {
        lock (_lock)
        {
            updateAction(_config);
            SaveConfig();
        }
        OnConfigChanged?.Invoke();
    }

    private void LoadConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                _config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading config: {ex.Message}");
            _config = new AppConfig();
        }
    }

    private void SaveConfig()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_config, options);
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving config: {ex.Message}");
        }
    }

    private void EnsureDirectories()
    {
        try
        {
            if (!Directory.Exists(ToolsDirectory))
                Directory.CreateDirectory(ToolsDirectory);

            if (!Directory.Exists(_config.OutputDirectory))
                Directory.CreateDirectory(_config.OutputDirectory);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating directories: {ex.Message}");
        }
    }

    public bool IsYtDlpInstalled()
    {
        return File.Exists(YtDlpPath) && new FileInfo(YtDlpPath).Length > 1000;
    }

    public bool IsFfmpegInstalled()
    {
        return File.Exists(FfmpegPath) && new FileInfo(FfmpegPath).Length > 1000;
    }
}
