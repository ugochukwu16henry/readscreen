namespace Readscreen.Core.Models;

public record DocumentChunk(
    Guid Id,
    Guid SessionId,
    string SourceFile,
    string Content,
    int ChunkIndex);
