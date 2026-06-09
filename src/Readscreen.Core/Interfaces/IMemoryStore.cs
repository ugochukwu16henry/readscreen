using Readscreen.Core.Models;

namespace Readscreen.Core.Interfaces;

public interface IMemoryStore
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(MemoryEntry entry, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MemoryEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MemoryEntry>> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default);
}
