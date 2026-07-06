using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DXF_Converter_GUI.ViewModels;

namespace DXF_Converter_GUI
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
        }

        private static readonly FilePickerFileType DxfType =
            new("DXF drawings") { Patterns = new[] { "*.dxf" } };

        private static readonly FilePickerFileType SvgType =
            new("SVG images") { Patterns = new[] { "*.svg" } };

        private static readonly FilePickerFileType PngType =
            new("PNG images") { Patterns = new[] { "*.png" } };

        // Rotate and download live on the shared PreviewPanel control; the window only handles
        // uploads (which set a node's artifact) and the format-changing transitions.

        private async void OnLoadDxf(object? sender, RoutedEventArgs e)
        {
            string? path = await PickOpenAsync("Load a DXF file", DxfType);
            if (path is not null)
            {
                _viewModel.LoadDxf(path);
            }
        }

        private async void OnLoadSvg(object? sender, RoutedEventArgs e)
        {
            string? path = await PickOpenAsync("Load an SVG file", SvgType);
            if (path is not null)
            {
                _viewModel.LoadSvg(path);
            }
        }

        private async void OnLoadPng(object? sender, RoutedEventArgs e)
        {
            string? path = await PickOpenAsync("Load a PNG file", PngType);
            if (path is not null)
            {
                _viewModel.LoadPng(path);
            }
        }

        private void OnConvertToSvg(object? sender, RoutedEventArgs e) => _viewModel.ConvertDxfToSvg();

        private void OnConvertToPng(object? sender, RoutedEventArgs e) => _viewModel.ConvertSvgToPng();

        private async Task<string?> PickOpenAsync(string title, FilePickerFileType type)
        {
            var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = new[] { type },
            });
            return result.Count > 0 ? result[0].TryGetLocalPath() : null;
        }
    }
}
