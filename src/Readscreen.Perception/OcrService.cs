using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Readscreen.Core.Interfaces;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace Readscreen.Perception;

public sealed class OcrService : IOcrService
{
    private readonly OcrEngine? _engine;

    public OcrService()
    {
        _engine = OcrEngine.TryCreateFromUserProfileLanguages();
    }

    public bool IsAvailable => _engine != null;

    public async Task<string> ExtractTextAsync(Bitmap image, CancellationToken cancellationToken = default)
    {
        if (_engine == null)
            return string.Empty;

        using var stream = new InMemoryRandomAccessStream();
        await ConvertToStreamAsync(image, stream);

        var decoder = await BitmapDecoder.CreateAsync(stream);
        var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied);

        var result = await _engine.RecognizeAsync(softwareBitmap);
        return result?.Text?.Trim() ?? string.Empty;
    }

    private static async Task ConvertToStreamAsync(Bitmap bitmap, IRandomAccessStream stream)
    {
        using var mem = new MemoryStream();
        bitmap.Save(mem, ImageFormat.Png);
        mem.Position = 0;
        await stream.WriteAsync(mem.ToArray().AsBuffer());
        stream.Seek(0);
    }
}
