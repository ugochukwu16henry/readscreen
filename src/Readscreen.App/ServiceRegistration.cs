using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Readscreen.Core.Interfaces;
using Readscreen.Core.Services;
using Readscreen.Llm;
using Readscreen.Memory;
using Readscreen.Overlay;
using Readscreen.Perception;
using Serilog;

namespace Readscreen.App;

public static class ServiceRegistration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IAppSettings, AppSettingsService>();
        services.AddSingleton<IOverlayService, OverlayService>();
        services.AddSingleton<ContextOrchestrator>();

        services.AddSingleton<IScreenCaptureService, ScreenCaptureService>();
        services.AddSingleton<IOcrService, OcrService>();
        services.AddSingleton<IAudioCaptureService, AudioLoopbackService>();

        services.AddHttpClient<ILlmClient, OllamaClient>();
        services.AddHttpClient<IEmbeddingService, EmbeddingService>();
        services.AddHttpClient<IAsrService, OllamaAsrService>();

        services.AddSingleton<IMemoryStore, SqliteMemoryStore>();
        services.AddSingleton<IDocumentStore, SqliteDocumentStore>();

        services.AddHostedService<ScreenMonitorWorker>();
        services.AddHostedService<AudioMonitorWorker>();
        services.AddHostedService<StartupService>();
    }

    public static void ConfigureLogging()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Readscreen", "logs");
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(Path.Combine(logDir, "readscreen-.log"), rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}

public sealed class StartupService : IHostedService
{
    private readonly IMemoryStore _memory;
    private readonly IDocumentStore _documents;
    private readonly IOverlayService _overlay;
    private readonly IAppSettings _settings;
    private readonly ILogger<StartupService> _logger;

    public StartupService(
        IMemoryStore memory,
        IDocumentStore documents,
        IOverlayService overlay,
        IAppSettings settings,
        ILogger<StartupService> logger)
    {
        _memory = memory;
        _documents = documents;
        _overlay = overlay;
        _settings = settings;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _memory.InitializeAsync(cancellationToken);
        await _documents.InitializeAsync(cancellationToken);

        _overlay.SetOpacity(_settings.Current.OverlayOpacity);
        _overlay.SetClickThrough(_settings.Current.ClickThrough);
        _overlay.UpdateText("Readscreen ready.\nMonitoring screen and audio...");
        _overlay.Show();

        _logger.LogInformation("Readscreen started");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
