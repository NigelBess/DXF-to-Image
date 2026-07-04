using System.Text.RegularExpressions;
using DxfToSvg.Core;
using SkiaSharp;

namespace DxfToSvg.Tests;

/// <summary>Coverage for the new SVG -> PNG stage (absent in the Python code): the PNG must
/// take a width only and derive its height from the SVG's aspect ratio.</summary>
public class RasterizationTests
{
    private static readonly string SampleDxf =
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Nebula Logo.dxf");

    [Fact]
    public void RendersPngAtRequestedWidthPreservingAspectRatio()
    {
        (string svg, _) = Converter.ConvertToSvg(SampleDxf);

        // Aspect ratio comes from the SVG viewBox.
        Match vb = Regex.Match(svg, "viewBox=\"0 0 ([0-9.]+) ([0-9.]+)\"");
        Assert.True(vb.Success, "generated SVG should have a viewBox");
        double vbW = double.Parse(vb.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        double vbH = double.Parse(vb.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);

        const int width = 500;
        int expectedHeight = (int)System.Math.Round(width * vbH / vbW);

        byte[] png = SvgRasterizer.RenderToPng(svg, width);
        using SKBitmap bitmap = SKBitmap.Decode(png);

        Assert.Equal(width, bitmap.Width);
        Assert.Equal(expectedHeight, bitmap.Height);
    }
}
