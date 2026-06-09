using System.Drawing;

namespace Readscreen.Core.Interfaces;

public interface IOcrService
{
    Task<string> ExtractTextAsync(Bitmap image, CancellationToken cancellationToken = default);
    bool IsAvailable { get; }
}
