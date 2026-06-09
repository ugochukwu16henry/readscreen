using Readscreen.Core.Models;

namespace Readscreen.Core.Interfaces;

public interface ILlmClient
{
    IAsyncEnumerable<string> StreamCompletionAsync(LlmRequest request, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
