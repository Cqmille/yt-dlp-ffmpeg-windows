using MediaGrabber.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register application services
builder.Services.AddSingleton<ConfigService>();
builder.Services.AddSingleton<YtDlpService>();
builder.Services.AddSingleton<FFmpegService>();
builder.Services.AddSingleton<PresetService>();
builder.Services.AddSingleton<QueueService>();
builder.Services.AddSingleton<UpdateService>();

// Register BackgroundService for queue processing
builder.Services.AddHostedService(sp => sp.GetRequiredService<QueueService>());

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<MediaGrabber.Components.App>()
    .AddInteractiveServerRenderMode();

// Get configured port or use default
var port = app.Configuration.GetValue<int>("Port", 5000);

Console.WriteLine($"");
Console.WriteLine($"  MediaGrabber is running!");
Console.WriteLine($"  Open your browser at: http://localhost:{port}");
Console.WriteLine($"  Press Ctrl+C to stop.");
Console.WriteLine($"");

app.Run($"http://localhost:{port}");
