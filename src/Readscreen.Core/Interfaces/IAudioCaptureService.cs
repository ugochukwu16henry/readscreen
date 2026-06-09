using Readscreen.Core.Models;

namespace Readscreen.Core.Interfaces;

public interface IAudioCaptureService
{
    IObservable<AudioChunk> CaptureLoopback();
    void Start();
    void Stop();
    bool IsCapturing { get; }
}
