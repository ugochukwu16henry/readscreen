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
    private WasapiLoopbackCapture? _capture;
    private MemoryStream? _buffer;
    private WaveFormat? _targetFormat;
    private bool _isCapturing;

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

        _capture = new WasapiLoopbackCapture();
        _targetFormat = new WaveFormat(16000, 16, 1);
        _buffer = new MemoryStream();

        _capture.DataAvailable += OnDataAvailable;
        _capture.RecordingStopped += (_, _) => _isCapturing = false;
        _capture.StartRecording();
        _isCapturing = true;
    }

    public void Stop()
    {
        if (!_isCapturing || _capture == null)
            return;

        _capture.StopRecording();
        _capture.DataAvailable -= OnDataAvailable;
        _capture.Dispose();
        _capture = null;
        _buffer?.Dispose();
        _buffer = null;
        _isCapturing = false;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_buffer == null || _capture == null || _targetFormat == null)
            return;

        _buffer.Write(e.Buffer, 0, e.BytesRecorded);

        var chunkBytes = _settings.Current.AudioChunkSeconds * _targetFormat.AverageBytesPerSecond;
        if (_buffer.Length < chunkBytes)
            return;

        var raw = _buffer.ToArray();
        _buffer.SetLength(0);

        var pcm = ConvertToPcm16Mono16kHz(raw, _capture.WaveFormat, _targetFormat);
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
        _subject.Dispose();
    }
}
