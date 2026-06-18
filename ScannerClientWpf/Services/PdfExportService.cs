using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScannerClientWpf.Services;

public sealed class PdfExportService
{
    public void Save(IReadOnlyList<BitmapSource> pages, string path)
    {
        if (pages.Count == 0)
            throw new InvalidOperationException("Nenhuma pagina para salvar.");

        var pageData = pages.Select(CreatePdfPage).ToList();
        var objectCount = 2 + pageData.Count * 3;
        var offsets = new long[objectCount + 1];

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        WriteAscii(writer, "%PDF-1.4\n");

        BeginObject(writer, stream, offsets, 1);
        WriteAscii(writer, "<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        BeginObject(writer, stream, offsets, 2);
        var kids = string.Join(" ", pageData.Select((_, index) => $"{3 + index * 3} 0 R"));
        WriteAscii(writer, $"<< /Type /Pages /Count {pageData.Count} /Kids [ {kids} ] >>\nendobj\n");

        for (var i = 0; i < pageData.Count; i++)
        {
            var data = pageData[i];
            var pageObject = 3 + i * 3;
            var contentObject = pageObject + 1;
            var imageObject = pageObject + 2;
            var imageName = $"Im{i}";
            var content = Encoding.ASCII.GetBytes(
                FormattableString.Invariant(
                    $"q\n{data.DrawWidth:0.###} 0 0 {data.DrawHeight:0.###} {data.OffsetX:0.###} {data.OffsetY:0.###} cm\n/{imageName} Do\nQ\n"));

            BeginObject(writer, stream, offsets, pageObject);
            WriteAscii(writer,
                FormattableString.Invariant(
                    $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {data.PageWidth:0.###} {data.PageHeight:0.###}] /Resources << /ProcSet [/PDF /ImageC] /XObject << /{imageName} {imageObject} 0 R >> >> /Contents {contentObject} 0 R >>\nendobj\n"));

            BeginObject(writer, stream, offsets, contentObject);
            WriteStreamObject(writer, $"<< /Length {content.Length} >>", content);

            BeginObject(writer, stream, offsets, imageObject);
            WriteStreamObject(writer,
                $"<< /Type /XObject /Subtype /Image /Width {data.PixelWidth} /Height {data.PixelHeight} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {data.JpegBytes.Length} >>",
                data.JpegBytes);
        }

        var xrefOffset = stream.Position;
        WriteAscii(writer, $"xref\n0 {objectCount + 1}\n");
        WriteAscii(writer, "0000000000 65535 f \n");
        for (var i = 1; i < offsets.Length; i++)
            WriteAscii(writer, $"{offsets[i]:0000000000} 00000 n \n");

        WriteAscii(writer, $"trailer\n<< /Size {objectCount + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
    }

    private static PdfPageData CreatePdfPage(BitmapSource source)
    {
        var image = NormalizeToRgb(source);
        var dpiX = image.DpiX > 0 ? image.DpiX : 96;
        var dpiY = image.DpiY > 0 ? image.DpiY : 96;
        var pageWidth = image.PixelWidth * 72.0 / dpiX;
        var pageHeight = image.PixelHeight * 72.0 / dpiY;

        return new PdfPageData(
            image.PixelWidth,
            image.PixelHeight,
            pageWidth,
            pageHeight,
            pageWidth,
            pageHeight,
            0,
            0,
            EncodeJpeg(image));
    }

    private static BitmapSource NormalizeToRgb(BitmapSource source)
    {
        if (source.Format == PixelFormats.Rgb24)
            return source;

        var converted = new FormatConvertedBitmap(source, PixelFormats.Rgb24, null, 0);
        converted.Freeze();
        return converted;
    }

    private static byte[] EncodeJpeg(BitmapSource source)
    {
        var encoder = new JpegBitmapEncoder { QualityLevel = 92 };
        encoder.Frames.Add(BitmapFrame.Create(source));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static void BeginObject(BinaryWriter writer, Stream stream, long[] offsets, int objectNumber)
    {
        offsets[objectNumber] = stream.Position;
        WriteAscii(writer, $"{objectNumber} 0 obj\n");
    }

    private static void WriteStreamObject(BinaryWriter writer, string dictionary, byte[] bytes)
    {
        WriteAscii(writer, dictionary + "\nstream\n");
        writer.Write(bytes);
        WriteAscii(writer, "\nendstream\nendobj\n");
    }

    private static void WriteAscii(BinaryWriter writer, string text)
    {
        writer.Write(Encoding.ASCII.GetBytes(text));
    }

    private sealed record PdfPageData(
        int PixelWidth,
        int PixelHeight,
        double PageWidth,
        double PageHeight,
        double DrawWidth,
        double DrawHeight,
        double OffsetX,
        double OffsetY,
        byte[] JpegBytes);
}
