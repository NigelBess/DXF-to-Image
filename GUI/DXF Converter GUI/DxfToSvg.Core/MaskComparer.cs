using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SkiaSharp;

namespace DxfToSvg.Core;

/// <summary>Port of <c>dxf_to_svg/compare.py</c>: a tolerant, dilation-based IoU between a
/// generated SVG (rasterized from its own polygons) and a reference PNG. Used by the tests.</summary>
public static class MaskComparer
{
    private static readonly Regex NumberPattern = new(@"-?\d+(?:\.\d+)?", RegexOptions.Compiled);
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    private static (List<List<(double X, double Y)>> Faces, (double MinX, double MinY, double Width, double Height) ViewBox)
        LoadSvgPaths(string svgPath)
    {
        XElement root = XDocument.Load(svgPath).Root!;
        string? viewBox = root.Attribute("viewBox")?.Value;
        if (string.IsNullOrWhiteSpace(viewBox))
        {
            throw new InvalidDataException("SVG is missing viewBox");
        }
        double[] vb = viewBox.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => double.Parse(part, CultureInfo.InvariantCulture)).ToArray();

        var faces = new List<List<(double, double)>>();
        foreach (XElement path in root.Descendants(Svg + "path"))
        {
            double[] numbers = NumberPattern.Matches(path.Attribute("d")!.Value)
                .Select(m => double.Parse(m.Value, CultureInfo.InvariantCulture)).ToArray();
            var points = new List<(double, double)>();
            for (int i = 0; i + 1 < numbers.Length; i += 2)
            {
                points.Add((numbers[i], numbers[i + 1]));
            }
            faces.Add(points);
        }
        return (faces, (vb[0], vb[1], vb[2], vb[3]));
    }

    private static (int MinX, int MinY, int MaxX, int MaxY) MaskBounds(HashSet<(int X, int Y)> mask)
    {
        if (mask.Count == 0)
        {
            throw new InvalidOperationException("Cannot compute bounds for an empty mask");
        }
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach ((int x, int y) in mask)
        {
            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }
        return (minX, minY, maxX, maxY);
    }

    private static HashSet<(int, int)> RenderSvgMask(
        string svgPath,
        (int Width, int Height) size,
        (int Left, int Top, int Right, int Bottom) targetBox)
    {
        (List<List<(double X, double Y)>> faces, (double MinX, double MinY, double Width, double Height) vb) = LoadSvgPaths(svgPath);

        double sx = (targetBox.Right - targetBox.Left + 1) / vb.Width;
        double sy = (targetBox.Bottom - targetBox.Top + 1) / vb.Height;
        double offsetX = targetBox.Left;
        double offsetY = targetBox.Top;

        var info = new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Black);
            using var paint = new SKPaint { Color = SKColors.White, IsAntialias = false, Style = SKPaintStyle.Fill };
            foreach (List<(double X, double Y)> face in faces)
            {
                if (face.Count < 3)
                {
                    continue;
                }
                using var path = new SKPath();
                path.MoveTo((float)(offsetX + face[0].X * sx), (float)(offsetY + face[0].Y * sy));
                for (int i = 1; i < face.Count; i++)
                {
                    path.LineTo((float)(offsetX + face[i].X * sx), (float)(offsetY + face[i].Y * sy));
                }
                path.Close();
                canvas.DrawPath(path, paint);
            }
        }

        var mask = new HashSet<(int, int)>();
        ReadOnlySpan<byte> pixels = bitmap.GetPixelSpan();
        int rowBytes = bitmap.RowBytes;
        for (int y = 0; y < size.Height; y++)
        {
            int row = y * rowBytes;
            for (int x = 0; x < size.Width; x++)
            {
                // Rgba8888: red byte is first in each 4-byte pixel. White fill => red > 0.
                if (pixels[row + x * 4] > 0)
                {
                    mask.Add((x, y));
                }
            }
        }
        return mask;
    }

    private static HashSet<(int, int)> ReferenceMask(string pngPath, int backgroundThreshold = 245)
    {
        using SKBitmap bitmap = SKBitmap.Decode(pngPath);
        var mask = new HashSet<(int, int)>();
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                SKColor c = bitmap.GetPixel(x, y);
                if (c.Alpha > 0 && Math.Min(c.Red, Math.Min(c.Green, c.Blue)) < backgroundThreshold)
                {
                    mask.Add((x, y));
                }
            }
        }
        return mask;
    }

    private static HashSet<(int, int)> Dilate(HashSet<(int, int)> mask, int width, int height, int radius)
    {
        if (radius <= 0)
        {
            return mask;
        }
        var grown = new HashSet<(int, int)>();
        foreach ((int x, int y) in mask)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        grown.Add((nx, ny));
                    }
                }
            }
        }
        return grown;
    }

    /// <summary>Symmetric, dilation-tolerant Intersection-over-Union of the SVG and PNG masks.</summary>
    public static double Compare(string svgPath, string pngPath, int toleranceRadius = 2)
    {
        HashSet<(int, int)> pngMask = ReferenceMask(pngPath);
        (int Width, int Height) size;
        using (SKBitmap bitmap = SKBitmap.Decode(pngPath))
        {
            size = (bitmap.Width, bitmap.Height);
        }

        (int minX, int minY, int maxX, int maxY) = MaskBounds(pngMask);
        HashSet<(int, int)> svgMask = RenderSvgMask(svgPath, size, (minX, minY, maxX, maxY));

        HashSet<(int, int)> svgWide = Dilate(svgMask, size.Width, size.Height, toleranceRadius);
        HashSet<(int, int)> pngWide = Dilate(pngMask, size.Width, size.Height, toleranceRadius);

        var intersection = new HashSet<(int, int)>();
        foreach ((int, int) p in svgMask)
        {
            if (pngWide.Contains(p))
            {
                intersection.Add(p);
            }
        }
        foreach ((int, int) p in pngMask)
        {
            if (svgWide.Contains(p))
            {
                intersection.Add(p);
            }
        }

        var union = new HashSet<(int, int)>(svgMask);
        union.UnionWith(pngMask);

        return union.Count > 0 ? (double)intersection.Count / union.Count : 1.0;
    }
}
