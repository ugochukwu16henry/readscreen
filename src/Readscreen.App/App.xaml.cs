using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Readscreen.Core.Interfaces;
using Readscreen.Overlay;
using Serilog;

namespace Readscreen.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private IHost? _host;
    private GlobalHotkeyService? _hotkeys;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ServiceRegistration.ConfigureLogging();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(services =>
            {
                ServiceRegistration.ConfigureServices(services);
                services.AddSingleton<MainWindow>();
                services.AddTransient<MemoryEditorWindow>();
            })
            .Build();

        Services = _host.Services;

        await _host.StartAsync();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        var overlay = Services.GetRequiredService<IOverlayService>() as OverlayService;

        _hotkeys = new GlobalHotkeyService();
        if (overlay != null)
            _hotkeys.Initialize(overlay.GetWindow());
        mainWindow.RegisterHotkeys(_hotkeys);

        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _hotkeys?.Dispose();
        if (_host != null)
            await _host.StopAsync();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
