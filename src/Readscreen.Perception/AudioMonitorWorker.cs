using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Readscreen.Core.Interfaces;
using Readscreen.Core.Models;
using Readscreen.Core.Services;

namespace Readscreen.Perception;

public sealed class AudioMonitorWorker : BackgroundService
{
    private readonly IAppSettings _settings;
    private readonly IAudioCaptureService _audio;
    private readonly IAsrService _asr;
    private readonly ContextOrchestrator _orchestrator;
    private readonly IOverlayService _overlay;
    private readonly ILogger<AudioMonitorWorker> _logger;
    private IDisposable? _subscription;

    public AudioMonitorWorker(
        IAppSettings settings,
        IAudioCaptureService audio,
        IAsrService asr,
        ContextOrchestrator orchestrator,
        IOverlayService overlay,
        ILogger<AudioMonitorWorker> logger)
    {
        _settings = settings;
        _audio = audio;
        _asr = asr;
        _orchestrator = orchestrator;
        _overlay = overlay;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Current.AudioEnabled)
            return Task.CompletedTask;

        _audio.Start();
        _overlay.SetStatus(AssistantStatus.Listening);

        _subscription = _audio.CaptureLoopback().Subscribe(async chunk =>
        {
            if (_orchestrator.IsPaused || stoppingToken.IsCancellationRequested)
                return;

            try
            {
                var transcript = await _asr.TranscribeAsync(chunk.Pcm16Mono16kHz, stoppingToken);
                if (string.IsNullOrWhiteSpace(transcript))
                    return;

                _logger.LogDebug("Transcript: {Text}", transcript);
                _orchestrator.OnTranscript(transcript);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ASR error");
            }
        });

        stoppingToken.Register(() =>
        {
            _audio.Stop();
            _subscription?.Dispose();
        });

        return Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { }, CancellationToken.None);
    }
}
