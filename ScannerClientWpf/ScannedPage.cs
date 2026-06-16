using System.Windows.Media.Imaging;

namespace ScannerClientWpf;

public sealed class ScannedPage
{
    public string Title { get; set; } = "";
    public BitmapSource Image { get; set; } = null!;
}
