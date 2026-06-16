using System.IO;
using System.Windows.Media.Imaging;

namespace ScannerClientWpf.Services;

public sealed class ImageEditService
{
    public BitmapSource Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    public BitmapSource Rotate(BitmapSource source, int angle)
    {
        var transform = new TransformedBitmap(source, new System.Windows.Media.RotateTransform(angle));
        transform.Freeze();
        return transform;
    }

    public void SaveImage(BitmapSource source, string path, string format)
    {
        BitmapEncoder encoder = format.Equals("PNG", StringComparison.OrdinalIgnoreCase)
            ? new PngBitmapEncoder()
            : new JpegBitmapEncoder { QualityLevel = 90 };

        encoder.Frames.Add(BitmapFrame.Create(source));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    public byte[] EncodeJpeg(BitmapSource source)
    {
        var encoder = new JpegBitmapEncoder { QualityLevel = 90 };
        encoder.Frames.Add(BitmapFrame.Create(source));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }
}
