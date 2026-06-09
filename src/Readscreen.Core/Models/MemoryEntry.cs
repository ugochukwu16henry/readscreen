namespace Readscreen.Core.Models;

public record MemoryEntry(
    Guid Id,
    string Category,
    string Title,
    string Content,
    DateTime UpdatedAt);
