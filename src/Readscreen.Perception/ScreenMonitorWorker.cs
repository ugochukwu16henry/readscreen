using System.Drawing;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Readscreen.Core.Interfaces;
using Readscreen.Core.Models;
using Readscreen.Core.Services;

namespace Readscreen.Perception;

public sealed class ScreenMonitorWorker : BackgroundService
{
    private readonly IAppSettings _settings;
    private readonly IScreenCaptureService _capture;
    private readonly IOcrService _ocr;
    private readonly ContextOrchestrator _orchestrator;
    private readonly IOverlayService _overlay;
    private readonly ILogger<ScreenMonitorWorker> _logger;
    private readonly ChangeDetector _changeDetector = new();

    public ScreenMonitorWorker(
        IAppSettings settings,
        IScreenCaptureService capture,
        IOcrService ocr,
        ContextOrchestrator orchestrator,
        IOverlayService overlay,
        ILogger<ScreenMonitorWorker> logger)
    {
        _settings = settings;
        _capture = capture;
        _ocr = ocr;
        _orchestrator = orchestrator;
        _overlay = overlay;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Screen monitor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_orchestrator.IsPaused)
                {
                    await Task.Delay(500, stoppingToken);
                    continue;
                }

                if (IsLockdownBrowserActive())
                {
                    _overlay.SetStatus(AssistantStatus.Blocked);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _settings.Current.PollIntervalSeconds)), stoppingToken);
                    continue;
                }

                var regionSettings = _settings.Current.CaptureRegion;
                var region = new CaptureRegion(
                    regionSettings.Top,
                    regionSettings.Left,
                    regionSettings.Width,
                    regionSettings.Height);

                _overlay.SetStatus(AssistantStatus.Reading);
                _overlay.SetListeningHint(
                    $"Watching {region.Width}x{region.Height} at ({region.Left},{region.Top})");

                using var bitmap = await _capture.CaptureRegionAsync(region, stoppingToken);
                var text = await _ocr.ExtractTextAsync(bitmap, stoppingToken);

                var debounce = _settings.Current.MeetingAssistEnabled
                    ? _settings.Current.MeetingAssistDebounceSeconds
                    : _settings.Current.DebounceSeconds;

                if (_changeDetector.HasMeaningfulChange(text, debounce))
                {
                    _logger.LogDebug("Screen text changed: {Length} chars", text.Length);
                    _orchestrator.OnScreenText(text);
                    _changeDetector.MarkProcessed(text);
                }

                if (_settings.Current.MeetingAssistEnabled)
                    _overlay.SetStatus(AssistantStatus.Listening);
                else
                    _overlay.SetStatus(AssistantStatus.Idle);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Screen monitor error");
                _overlay.SetStatus(AssistantStatus.Error);
            }

            var delay = TimeSpan.FromSeconds(_settings.Current.PollIntervalSeconds);
            await Task.Delay(delay, stoppingToken);
        }
    }

    private static bool IsLockdownBrowserActive()
    {
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (LockdownBrowserDetector.IsLikelyLockdownBrowser(process.ProcessName) ||
                    LockdownBrowserDetector.IsLikelyLockdownBrowser(process.MainWindowTitle))
                {
                    return true;
                }
            }
            catch
            {
                // Ignore processes that exit or deny access while we inspect them.
            }
            finally
            {
                process.Dispose();
            }
        }

        return false;
    }
}
