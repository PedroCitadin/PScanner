using Microsoft.Win32;
using ScannerClientWpf.Services;
using ScannerShared;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ScannerClientWpf;

public partial class MainWindow : Window
{
    private readonly ClientConfigService _configService = new();
    private readonly ApiClientService _api = new();
    private readonly ImageEditService _imageEdit = new();
    private readonly PdfExportService _pdfExport = new();
    private readonly ClientConfig _config;

    public ObservableCollection<ScannedPage> Pages { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        _config = _configService.Load();
        ServerUrlBox.Text = _config.LastServerUrl;
        DpiCombo.ItemsSource = new[] { 150, 200, 300 };
        DpiCombo.SelectedItem = _config.LastDpi;
        ColorModeCombo.ItemsSource = new[]
        {
            new ComboItem("Colorido", "Color"),
            new ComboItem("Cinza", "Grayscale"),
            new ComboItem("Preto e Branco", "BlackAndWhite")
        };
        ColorModeCombo.DisplayMemberPath = "Label";
        ColorModeCombo.SelectedIndex = Math.Max(0, Array.FindIndex(((ComboItem[])ColorModeCombo.ItemsSource), x => x.Value == _config.LastColorMode));
        FormatCombo.ItemsSource = new[] { "PDF", "PNG", "JPG" };
        FormatCombo.SelectedItem = _config.LastOutputFormat;
        UpdatePreview();
    }

    private async void TestButton_Click(object sender, RoutedEventArgs e)
    {
        await RunUiAsync("Testando conexao...", async token =>
        {
            _api.ServerUrl = ServerUrlBox.Text;
            var status = await _api.GetStatusAsync(token);
            StatusText.Text = $"Online: {status.ServerName} - {status.DateTime:dd/MM/yyyy HH:mm:ss}";
            SavePreferences();
        });
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshScannersAsync();
    }

    private async Task RefreshScannersAsync()
    {
        await RunUiAsync("Buscando scanners...", async token =>
        {
            _api.ServerUrl = ServerUrlBox.Text;
            var scanners = await _api.GetScannersAsync(token);
            ScannerCombo.ItemsSource = scanners;
            ScannerCombo.SelectedIndex = scanners.Count > 0 ? 0 : -1;
            StatusText.Text = scanners.Count == 0 ? "Nenhum scanner encontrado no servidor." : $"{scanners.Count} scanner(s) encontrado(s).";
            SavePreferences();
        });
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        if (ScannerCombo.SelectedItem is not ScannerInfoDto scanner)
        {
            MessageBox.Show("Selecione um scanner.", "Shared Scanner", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await RunUiAsync("Digitalizando...", async token =>
        {
            _api.ServerUrl = ServerUrlBox.Text;
            var request = new ScanRequestDto(scanner.Id, (int)DpiCombo.SelectedItem, ((ComboItem)ColorModeCombo.SelectedItem).Value, "A4");
            var result = await _api.ScanAsync(request, token);
            var bytes = await _api.DownloadScanAsync(result.DownloadUrl, token);
            var image = _imageEdit.Load(bytes);
            Pages.Add(new ScannedPage { Title = $"Pagina {Pages.Count + 1}", Image = image });
            PagesList.SelectedIndex = Pages.Count - 1;
            StatusText.Text = $"Digitalizacao recebida: {result.FileName}";
            SavePreferences();
        });
    }

    private void PagesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => UpdatePreview();

    private void RotateLeft_Click(object sender, RoutedEventArgs e) => RotateSelected(-90);
    private void RotateRight_Click(object sender, RoutedEventArgs e) => RotateSelected(90);

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        var index = PagesList.SelectedIndex;
        if (index < 0) return;
        Pages.RemoveAt(index);
        RenumberPages();
        PagesList.SelectedIndex = Math.Min(index, Pages.Count - 1);
        UpdatePreview();
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        var index = PagesList.SelectedIndex;
        if (index <= 0) return;
        Pages.Move(index, index - 1);
        RenumberPages();
        PagesList.SelectedIndex = index - 1;
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        var index = PagesList.SelectedIndex;
        if (index < 0 || index >= Pages.Count - 1) return;
        Pages.Move(index, index + 1);
        RenumberPages();
        PagesList.SelectedIndex = index + 1;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (Pages.Count == 0)
        {
            MessageBox.Show("Digitalize pelo menos uma pagina antes de salvar.", "Shared Scanner", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var format = (string)FormatCombo.SelectedItem;
        var dialog = new SaveFileDialog
        {
            InitialDirectory = Directory.Exists(_config.LastDestinationFolder) ? _config.LastDestinationFolder : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            FileName = "digitalizacao",
            Filter = format == "PDF" ? "PDF (*.pdf)|*.pdf" : $"{format} (*.{format.ToLowerInvariant()})|*.{format.ToLowerInvariant()}"
        };

        if (dialog.ShowDialog(this) != true) return;

        try
        {
            if (format == "PDF")
            {
                _pdfExport.Save(Pages.Select(p => p.Image).ToList(), dialog.FileName);
            }
            else
            {
                SaveImageSet(dialog.FileName, format);
            }

            _config.LastDestinationFolder = Path.GetDirectoryName(dialog.FileName) ?? _config.LastDestinationFolder;
            SavePreferences();
            StatusText.Text = $"Arquivo salvo em {dialog.FileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Falha ao salvar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveImageSet(string chosenPath, string format)
    {
        var folder = Path.GetDirectoryName(chosenPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var name = Path.GetFileNameWithoutExtension(chosenPath);
        var extension = format.Equals("PNG", StringComparison.OrdinalIgnoreCase) ? ".png" : ".jpg";

        for (var i = 0; i < Pages.Count; i++)
        {
            var path = Pages.Count == 1
                ? Path.Combine(folder, name + extension)
                : Path.Combine(folder, $"{name}_{i + 1:000}{extension}");
            _imageEdit.SaveImage(Pages[i].Image, path, format);
        }
    }

    private void RotateSelected(int angle)
    {
        if (PagesList.SelectedItem is not ScannedPage page) return;
        page.Image = _imageEdit.Rotate(page.Image, angle);
        var index = PagesList.SelectedIndex;
        Pages.RemoveAt(index);
        Pages.Insert(index, page);
        PagesList.SelectedIndex = index;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var page = PagesList.SelectedItem as ScannedPage;
        PreviewImage.Source = page?.Image;
        EmptyPreviewText.Visibility = page is null ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RenumberPages()
    {
        for (var i = 0; i < Pages.Count; i++)
            Pages[i].Title = $"Pagina {i + 1}";
        PagesList.Items.Refresh();
    }

    private async Task RunUiAsync(string busyText, Func<CancellationToken, Task> action)
    {
        SetBusy(true, busyText);
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(6));
            await action(cts.Token);
        }
        catch (Exception ex)
        {
            StatusText.Text = "Falha na operacao.";
            MessageBox.Show(ex.Message, "Shared Scanner", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false, StatusText.Text);
        }
    }

    private void SetBusy(bool busy, string text)
    {
        StatusText.Text = text;
        TestButton.IsEnabled = !busy;
        RefreshButton.IsEnabled = !busy;
        ScanButton.IsEnabled = !busy;
    }

    private void SavePreferences()
    {
        _config.LastServerUrl = ServerUrlBox.Text;
        _config.LastDpi = (int)(DpiCombo.SelectedItem ?? 200);
        _config.LastColorMode = ((ComboItem?)ColorModeCombo.SelectedItem)?.Value ?? "Color";
        _config.LastOutputFormat = (string)(FormatCombo.SelectedItem ?? "PDF");
        _configService.Save(_config);
    }

    private sealed record ComboItem(string Label, string Value);
}
