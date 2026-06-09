using System.Drawing;
using Readscreen.Core.Models;

namespace Readscreen.Core.Interfaces;

public interface IScreenCaptureService
{
    Task<Bitmap> CaptureRegionAsync(CaptureRegion region, CancellationToken cancellationToken = default);
    Task<Bitmap> CapturePrimaryScreenAsync(CancellationToken cancellationToken = default);
}
