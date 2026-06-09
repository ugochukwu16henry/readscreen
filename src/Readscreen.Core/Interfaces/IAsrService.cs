namespace Readscreen.Core.Interfaces;

public interface IAsrService
{
    Task<string> TranscribeAsync(byte[] pcm16Mono16kHz, CancellationToken cancellationToken = default);
    bool IsAvailable { get; }
}
