using Readscreen.Core.Models;

namespace Readscreen.Core.Interfaces;

public interface IDocumentStore
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<Guid> CreateSessionAsync(string name, CancellationToken cancellationToken = default);
    Task IngestAsync(Guid sessionId, string filePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentChunk>> SearchAsync(string query, Guid sessionId, int topK = 5, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetSessionFilesAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
