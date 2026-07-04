using SkiaSharp;
using Svg.Skia;

namespace DxfToSvg.Core;

/// <summary>Renders an SVG to a PNG at a target width (aspect ratio preserved).
/// This capability does not exist in the Python code; it uses a real SVG renderer
/// (Svg.Skia) so arbitrary uploaded SVGs render correctly.</summary>
public static class SvgRasterizer
{
    /// <summary>Renders SVG content to PNG bytes. The height is derived from the target
    /// width and the SVG's aspect ratio: <c>height = round(width * vbHeight / vbWidth)</c>.</summary>
    public static byte[] RenderToPng(string svgContent, int width)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive");
        }

        using var svg = new SKSvg();
        SKPicture? picture = svg.FromSvg(svgContent);
        if (picture is null)
        {
            throw new InvalidDataException("Could not parse SVG content");
        }

        SKRect cull = picture.CullRect;
        if (cull.Width <= 0 || cull.Height <= 0)
        {
            throw new InvalidDataException("SVG has no drawable area (empty viewBox)");
        }

        int height = Math.Max(1, (int)Math.Round(width * (double)cull.Height / cull.Width));
        float scale = width / cull.Width;

        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(scale);
        canvas.Translate(-cull.Left, -cull.Top);
        canvas.DrawPicture(picture);
        canvas.Flush();

        using SKImage image = surface.Snapshot();
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>Convenience helper: read an SVG file and write a PNG file.</summary>
    public static void RenderFileToPng(string svgPath, string pngPath, int width)
    {
        byte[] png = RenderToPng(File.ReadAllText(svgPath), width);
        File.WriteAllBytes(pngPath, png);
    }
}
