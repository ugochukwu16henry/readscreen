using System.IO;
using System.Reactive.Subjects;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Readscreen.Core.Interfaces;
using Readscreen.Core.Models;

namespace Readscreen.Perception;

public sealed class AudioLoopbackService : IAudioCaptureService, IDisposable
{
    private readonly IAppSettings _settings;
    private readonly Subject<AudioChunk> _subject = new();
    private WasapiLoopbackCapture? _loopbackCapture;
    private WasapiCapture? _microphoneCapture;
    private MemoryStream? _buffer;
    private WaveFormat? _sourceFormat;
    private WaveFormat? _targetFormat;
    private bool _isCapturing;
    private readonly MMDeviceEnumerator _deviceEnumerator = new();

    public AudioLoopbackService(IAppSettings settings)
    {
        _settings = settings;
    }

    public bool IsCapturing => _isCapturing;

    public IObservable<AudioChunk> CaptureLoopback() => _subject;

    public void Start()
    {
        if (_isCapturing)
            return;

        _targetFormat = new WaveFormat(16000, 16, 1);
        _buffer = new MemoryStream();

        switch (_settings.Current.AudioInputMode)
        {
            case AudioInputMode.Microphone:
                _microphoneCapture = CreateMicrophoneCapture();
                _sourceFormat = _microphoneCapture.WaveFormat;
                _microphoneCapture.DataAvailable += OnDataAvailable;
                _microphoneCapture.RecordingStopped += OnRecordingStopped;
                _microphoneCapture.StartRecording();
                break;
            default:
                _loopbackCapture = new WasapiLoopbackCapture();
                _sourceFormat = _loopbackCapture.WaveFormat;
                _loopbackCapture.DataAvailable += OnDataAvailable;
                _loopbackCapture.RecordingStopped += OnRecordingStopped;
                _loopbackCapture.StartRecording();
                break;
        }

        _isCapturing = true;
    }

    public void Stop()
    {
        if (!_isCapturing)
            return;

        if (_loopbackCapture != null)
        {
            _loopbackCapture.StopRecording();
            _loopbackCapture.DataAvailable -= OnDataAvailable;
            _loopbackCapture.RecordingStopped -= OnRecordingStopped;
            _loopbackCapture.Dispose();
            _loopbackCapture = null;
        }

        if (_microphoneCapture != null)
        {
            _microphoneCapture.StopRecording();
            _microphoneCapture.DataAvailable -= OnDataAvailable;
            _microphoneCapture.RecordingStopped -= OnRecordingStopped;
            _microphoneCapture.Dispose();
            _microphoneCapture = null;
        }

        _buffer?.Dispose();
        _buffer = null;
        _sourceFormat = null;
        _isCapturing = false;
    }

    private WasapiCapture CreateMicrophoneCapture()
    {
        var device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
        return new WasapiCapture(device);
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        _isCapturing = false;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_buffer == null || _sourceFormat == null || _targetFormat == null)
            return;

        _buffer.Write(e.Buffer, 0, e.BytesRecorded);

        var settings = _settings.Current;
        var chunkSeconds = settings.MeetingAssistEnabled
            ? Math.Clamp(settings.MeetingAssistAudioChunkSeconds, 1, 30)
            : Math.Clamp(settings.AudioChunkSeconds, 1, 30);
        var chunkBytes = chunkSeconds * _targetFormat.AverageBytesPerSecond;
        if (_buffer.Length < chunkBytes)
            return;

        var raw = _buffer.ToArray();
        _buffer.SetLength(0);

        var pcm = ConvertToPcm16Mono16kHz(raw, _sourceFormat, _targetFormat);
        if (pcm.Length > 0)
            _subject.OnNext(new AudioChunk(pcm, DateTime.UtcNow));
    }

    private static byte[] ConvertToPcm16Mono16kHz(byte[] input, WaveFormat source, WaveFormat target)
    {
        try
        {
            using var inputStream = new RawSourceWaveStream(input, 0, input.Length, source);
            using var resampler = new MediaFoundationResampler(inputStream, target) { ResamplerQuality = 60 };
            using var outStream = new MemoryStream();
            var buffer = new byte[target.AverageBytesPerSecond];
            int read;
            while ((read = resampler.Read(buffer, 0, buffer.Length)) > 0)
                outStream.Write(buffer, 0, read);
            return outStream.ToArray();
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    public void Dispose()
    {
        Stop();
        _deviceEnumerator.Dispose();
        _subject.Dispose();
    }
}
