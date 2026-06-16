using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace ScannerClientWpf.Services;

public sealed class PdfExportService
{
    private readonly ImageEditService _images = new();

    public void Save(IReadOnlyList<BitmapSource> pages, string path)
    {
        if (pages.Count == 0)
            throw new InvalidOperationException("Nenhuma pagina para salvar.");

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        var offsets = new List<long> { 0 };
        var objects = new List<Action>();

        objects.Add(() => WriteAscii(writer, "<< /Type /Catalog /Pages 2 0 R >>"));
        objects.Add(() =>
        {
            var kids = string.Join(" ", Enumerable.Range(0, pages.Count).Select(i => $"{3 + i * 3} 0 R"));
            WriteAscii(writer, $"<< /Type /Pages /Count {pages.Count} /Kids [ {kids} ] >>");
        });

        for (var i = 0; i < pages.Count; i++)
        {
            var pageObject = 3 + i * 3;
            var contentObject = pageObject + 1;
            var imageObject = pageObject + 2;
            var page = pages[i];
            var width = page.PixelWidth;
            var height = page.PixelHeight;
            var jpeg = _images.EncodeJpeg(page);
            var content = Encoding.ASCII.GetBytes($"q\n{width} 0 0 {height} 0 0 cm\n/Im{i} Do\nQ\n");

            objects.Add(() => WriteAscii(writer,
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {width} {height}] /Resources << /XObject << /Im{i} {imageObject} 0 R >> >> /Contents {contentObject} 0 R >>"));
            objects.Add(() => WriteStreamObject(writer, "<< /Length " + content.Length + " >>", content));
            objects.Add(() => WriteStreamObject(writer,
                $"<< /Type /XObject /Subtype /Image /Width {width} /Height {height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {jpeg.Length} >>",
                jpeg));
        }

        WriteAscii(writer, "%PDF-1.4\n");
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(stream.Position);
            WriteAscii(writer, $"{i + 1} 0 obj\n");
            objects[i]();
            WriteAscii(writer, "\nendobj\n");
        }

        var xrefOffset = stream.Position;
        WriteAscii(writer, $"xref\n0 {objects.Count + 1}\n");
        WriteAscii(writer, "0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            WriteAscii(writer, $"{offset:0000000000} 00000 n \n");
        WriteAscii(writer, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
    }

    private static void WriteStreamObject(BinaryWriter writer, string dictionary, byte[] bytes)
    {
        WriteAscii(writer, dictionary + "\nstream\n");
        writer.Write(bytes);
        WriteAscii(writer, "\nendstream");
    }

    private static void WriteAscii(BinaryWriter writer, string text)
    {
        writer.Write(Encoding.ASCII.GetBytes(text));
    }
}
