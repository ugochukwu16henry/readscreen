namespace Readscreen.Core.Models;

public record AudioChunk(byte[] Pcm16Mono16kHz, DateTime Timestamp);
