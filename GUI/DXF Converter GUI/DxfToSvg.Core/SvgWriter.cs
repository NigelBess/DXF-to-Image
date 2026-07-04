using System.Globalization;
using System.Text;

namespace DxfToSvg.Core;

/// <summary>Port of <c>dxf_to_svg/svg.py</c>: coordinate transform and SVG assembly.</summary>
public static class SvgWriter
{
    private static string Fmt(double value)
    {
        string text = value.ToString("F6", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
        return text.Length > 0 ? text : "0";
    }

    /// <summary>Flips BOTH X and Y (a 180-degree rotation) and applies padding, matching Python.</summary>
    private static Point Transform(Point point, Bounds bounds, double padding)
        => new(bounds.MaxX - point.X + padding, bounds.MaxY - point.Y + padding);

    public static string PathData(IReadOnlyList<Point> face, Bounds bounds, double padding)
    {
        var points = face.Select(p => Transform(p, bounds, padding)).ToList();
        var commands = new List<string> { $"M {Fmt(points[0].X)} {Fmt(points[0].Y)}" };
        for (int i = 1; i < points.Count; i++)
        {
            commands.Add($"L {Fmt(points[i].X)} {Fmt(points[i].Y)}");
        }
        commands.Add("Z");
        return string.Join(" ", commands);
    }

    public static string SvgDocument(
        IReadOnlyList<IReadOnlyList<Point>> faces,
        string fill,
        bool debugStrokes = false,
        double padding = 0.0)
    {
        if (faces.Count == 0)
        {
            throw new InvalidOperationException("No faces found to write");
        }
        var allPoints = faces.SelectMany(face => face).ToList();
        Bounds bounds = Geometry.BoundsForPoints(allPoints);
        double width = bounds.Width + padding * 2;
        double height = bounds.Height + padding * 2;
        string stroke = debugStrokes ? " stroke=\"#111827\" stroke-width=\"0.15\"" : " stroke=\"none\"";
        var pathBuilder = new StringBuilder();
        for (int i = 0; i < faces.Count; i++)
        {
            if (i > 0)
            {
                pathBuilder.Append('\n');
            }
            pathBuilder.Append($"  <path d=\"{Escape(PathData(faces[i], bounds, padding))}\" fill=\"{Escape(fill)}\"");
            pathBuilder.Append($" fill-rule=\"evenodd\"{stroke}/>");
        }

        return
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {Fmt(width)} {Fmt(height)}\" " +
            $"width=\"{Fmt(width)}\" height=\"{Fmt(height)}\">\n" +
            $"{pathBuilder}\n" +
            "</svg>\n";
    }

    /// <summary>Equivalent to Python's html.escape(quote=True).</summary>
    private static string Escape(string value) => value
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;")
        .Replace("'", "&#x27;");
}
