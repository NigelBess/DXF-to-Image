using System.Text.RegularExpressions;
using DxfToSvg.Core;
using SkiaSharp;

namespace DxfToSvg.Tests;

/// <summary>Coverage for the "rotate 90° clockwise" step added to each GUI stage.</summary>
public class TransformsTests
{
    private static readonly string SampleDxf =
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Nebula Logo.dxf");

    private static (double W, double H) ViewBoxSize(string svg)
    {
        Match vb = Regex.Match(svg, "viewBox=\"0 0 ([0-9.]+) ([0-9.]+)\"");
        Assert.True(vb.Success, "SVG should have an origin-anchored viewBox");
        var c = System.Globalization.CultureInfo.InvariantCulture;
        return (double.Parse(vb.Groups[1].Value, c), double.Parse(vb.Groups[2].Value, c));
    }

    [Fact]
    public void RotatingSvgSwapsViewBoxDimensions()
    {
        (string svg, _) = Converter.ConvertToSvg(SampleDxf);
        (double w, double h) = ViewBoxSize(svg);

        string rotated = Transforms.RotateSvgClockwise90(svg);
        (double rw, double rh) = ViewBoxSize(rotated);

        Assert.Equal(h, rw, 3);
        Assert.Equal(w, rh, 3);
    }

    [Fact]
    public void RotatedSvgStillRenders()
    {
        (string svg, _) = Converter.ConvertToSvg(SampleDxf);
        string rotated = Transforms.RotateSvgClockwise90(svg);

        // Rendering exercises the rotation transform through a real SVG renderer.
        byte[] png = SvgRasterizer.RenderToPng(rotated, 400);
        using SKBitmap bitmap = SKBitmap.Decode(png);
        Assert.Equal(400, bitmap.Width);
        Assert.True(bitmap.Height > 0);
    }

    [Fact]
    public void FourSvgRotationsRestoreOriginalDimensions()
    {
        (string svg, _) = Converter.ConvertToSvg(SampleDxf);
        (double w, double h) = ViewBoxSize(svg);

        string r = svg;
        for (int i = 0; i < 4; i++)
        {
            r = Transforms.RotateSvgClockwise90(r);
        }
        (double rw, double rh) = ViewBoxSize(r);

        Assert.Equal(w, rw, 3);
        Assert.Equal(h, rh, 3);
    }

    [Fact]
    public void RotatingPngSwapsWidthAndHeight()
    {
        (string svg, _) = Converter.ConvertToSvg(SampleDxf);
        byte[] png = SvgRasterizer.RenderToPng(svg, 300);
        using SKBitmap original = SKBitmap.Decode(png);

        byte[] rotatedPng = Transforms.RotatePngClockwise90(png);
        using SKBitmap rotated = SKBitmap.Decode(rotatedPng);

        Assert.Equal(original.Height, rotated.Width);
        Assert.Equal(original.Width, rotated.Height);
    }

    [Fact]
    public void RotatingPngClockwiseMovesTopLeftPixelToTopRight()
    {
        // Build a 2x1 image: left pixel red, right pixel blue.
        using var src = new SKBitmap(2, 1, SKColorType.Rgba8888, SKAlphaType.Premul);
        src.SetPixel(0, 0, SKColors.Red);
        src.SetPixel(1, 0, SKColors.Blue);
        using SKImage srcImage = SKImage.FromBitmap(src);
        using SKData srcData = srcImage.Encode(SKEncodedImageFormat.Png, 100);

        byte[] rotatedPng = Transforms.RotatePngClockwise90(srcData.ToArray());
        using SKBitmap rotated = SKBitmap.Decode(rotatedPng);

        // 2 wide x 1 tall rotated CW -> 1 wide x 2 tall; original top-left (red) lands top-right,
        // which in a 1px-wide image is the top row.
        Assert.Equal(1, rotated.Width);
        Assert.Equal(2, rotated.Height);
        Assert.Equal(SKColors.Red, rotated.GetPixel(0, 0));
        Assert.Equal(SKColors.Blue, rotated.GetPixel(0, 1));
    }
}
