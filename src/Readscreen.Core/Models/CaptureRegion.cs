namespace Readscreen.Core.Models;

public record CaptureRegion(int Top, int Left, int Width, int Height)
{
    public static CaptureRegion FullPrimary(int width, int height) =>
        new(0, 0, width, height);
}
