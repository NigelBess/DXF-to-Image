using System;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using DxfToSvg.Core;

namespace DXF_Converter_GUI.ViewModels;

/// <summary>Drives the DXF -> SVG -> PNG pipeline. Holds the state for both stages and
/// runs the conversions via <see cref="DxfToSvg.Core"/>. File dialogs live in the view;
/// this type is handed the resulting paths/streams.</summary>
public partial class MainWindowViewModel : ObservableObject
{
    private const int PreviewWidth = 640;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConvertToSvg))]
    private string? _dxfFileName;

    private string? _dxfPath;

    [ObservableProperty]
    private string _fillColor = Converter.DefaultFill;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSvg))]
    [NotifyPropertyChangedFor(nameof(CanConvertToPng))]
    private string? _svgContent;

    [ObservableProperty]
    private Bitmap? _svgPreview;

    [ObservableProperty]
    private int _pngWidth = 813;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPng))]
    private byte[]? _pngBytes;

    [ObservableProperty]
    private Bitmap? _pngPreview;

    [ObservableProperty]
    private string _status = "Load a DXF (stage 1) or an SVG (stage 2) to begin.";

    /// <summary>File name (without extension) of the most recently loaded input, used to
    /// suggest download names. Null until a DXF or SVG is loaded.</summary>
    public string? SourceBaseName { get; private set; }

    public bool HasSvg => !string.IsNullOrEmpty(SvgContent);
    public bool HasPng => PngBytes is not null;
    public bool CanConvertToSvg => !string.IsNullOrEmpty(_dxfPath);
    public bool CanConvertToPng => HasSvg;

    public void LoadDxf(string path)
    {
        _dxfPath = path;
        DxfFileName = Path.GetFileName(path);
        SourceBaseName = Path.GetFileNameWithoutExtension(path);
        Status = $"Loaded DXF \"{DxfFileName}\". Set a fill color and convert to SVG.";
        OnPropertyChanged(nameof(CanConvertToSvg));
    }

    public void ConvertDxfToSvg()
    {
        if (string.IsNullOrEmpty(_dxfPath))
        {
            return;
        }
        try
        {
            string fill = string.IsNullOrWhiteSpace(FillColor) ? Converter.DefaultFill : FillColor;
            (string svg, int faceCount) = Converter.ConvertToSvg(_dxfPath, fill);
            SvgContent = svg;
            UpdateSvgPreview();
            Status = $"Converted to SVG: {faceCount} filled face(s). Download it, or render to PNG.";
        }
        catch (Exception ex)
        {
            Status = $"DXF -> SVG failed: {ex.Message}";
        }
    }

    public void LoadSvg(string path)
    {
        try
        {
            SvgContent = File.ReadAllText(path);
            SourceBaseName = Path.GetFileNameWithoutExtension(path);
            UpdateSvgPreview();
            Status = $"Loaded SVG \"{Path.GetFileName(path)}\". Set a width and render to PNG.";
        }
        catch (Exception ex)
        {
            Status = $"Loading SVG failed: {ex.Message}";
        }
    }

    public void ConvertSvgToPng()
    {
        if (string.IsNullOrEmpty(SvgContent))
        {
            return;
        }
        if (PngWidth <= 0)
        {
            Status = "PNG width must be a positive number.";
            return;
        }
        try
        {
            byte[] bytes = SvgRasterizer.RenderToPng(SvgContent, PngWidth);
            PngBytes = bytes;
            PngPreview = LoadBitmap(bytes);
            Status = $"Rendered PNG at {PngWidth}px wide. Ready to download.";
        }
        catch (Exception ex)
        {
            Status = $"SVG -> PNG failed: {ex.Message}";
        }
    }

    private void UpdateSvgPreview()
    {
        if (string.IsNullOrEmpty(SvgContent))
        {
            SvgPreview = null;
            return;
        }
        try
        {
            SvgPreview = LoadBitmap(SvgRasterizer.RenderToPng(SvgContent, PreviewWidth));
        }
        catch
        {
            SvgPreview = null;
        }
    }

    private static Bitmap LoadBitmap(byte[] png)
    {
        using var stream = new MemoryStream(png);
        return new Bitmap(stream);
    }
}
