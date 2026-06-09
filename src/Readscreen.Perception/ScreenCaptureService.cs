using System.Drawing;
using Readscreen.Core.Interfaces;
using Readscreen.Core.Models;

namespace Readscreen.Perception;

public sealed class ScreenCaptureService : IScreenCaptureService
{
    public Task<Bitmap> CaptureRegionAsync(CaptureRegion region, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var bitmap = new Bitmap(region.Width, region.Height);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(region.Left, region.Top, 0, 0, new Size(region.Width, region.Height));
            return bitmap;
        }, cancellationToken);
    }

    public Task<Bitmap> CapturePrimaryScreenAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen?.Bounds
                         ?? throw new InvalidOperationException("No primary screen found.");
            return CaptureRegionAsync(
                new CaptureRegion(bounds.Top, bounds.Left, bounds.Width, bounds.Height),
                cancellationToken).GetAwaiter().GetResult();
        }, cancellationToken);
    }
}
