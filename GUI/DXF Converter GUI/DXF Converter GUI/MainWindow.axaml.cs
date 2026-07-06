using System;
using System.IO;
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

        private async void OnLoadDxf(object? sender, RoutedEventArgs e)
        {
            string? path = await PickOpenAsync("Load a DXF file", DxfType);
            if (path is not null)
            {
                _viewModel.LoadDxf(path);
            }
        }

        private void OnConvertToSvg(object? sender, RoutedEventArgs e) => _viewModel.ConvertDxfToSvg();

        private void OnRotateSvg(object? sender, RoutedEventArgs e) => _viewModel.RotateSvgClockwise();

        private async void OnDownloadSvg(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_viewModel.SvgContent))
            {
                return;
            }
            string? path = await PickSaveAsync("Save SVG", SvgType, $"{_viewModel.SourceBaseName ?? "output"}.svg");
            if (path is not null)
            {
                await File.WriteAllTextAsync(path, _viewModel.SvgContent);
                _viewModel.Status = $"Saved SVG to {path}";
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

        private void OnConvertToPng(object? sender, RoutedEventArgs e) => _viewModel.ConvertSvgToPng();

        private void OnRotatePng(object? sender, RoutedEventArgs e) => _viewModel.RotatePngClockwise();

        private async void OnDownloadPng(object? sender, RoutedEventArgs e)
        {
            if (_viewModel.PngBytes is null)
            {
                return;
            }
            string? path = await PickSaveAsync("Save PNG", PngType, $"{_viewModel.SourceBaseName ?? "output"}.png");
            if (path is not null)
            {
                await File.WriteAllBytesAsync(path, _viewModel.PngBytes);
                _viewModel.Status = $"Saved PNG to {path}";
            }
        }

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

        private async Task<string?> PickSaveAsync(string title, FilePickerFileType type, string suggestedName)
        {
            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = suggestedName,
                DefaultExtension = Path.GetExtension(suggestedName).TrimStart('.'),
                FileTypeChoices = new[] { type },
            });
            return file?.TryGetLocalPath();
        }
    }
}
