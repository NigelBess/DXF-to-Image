using System.Globalization;
using System.Text;

namespace DxfToSvg.Core;

/// <summary>Renders a raw DXF (its line/arc geometry as strokes) to a preview PNG. Unlike
/// <see cref="Converter"/> this does not trace faces or fill regions — it shows the drawing's
/// linework so a DXF can be previewed before it is converted to a filled SVG. Works even when
/// the geometry does not form closed loops.</summary>
public static class DxfPreviewRenderer
{
    /// <summary>Parses and flattens a DXF file, emits a stroke-only SVG of the raw segments,
    /// and rasterizes it to PNG bytes at the target width.</summary>
    public static byte[] RenderToPng(string dxfPath, int width)
        => SvgRasterizer.RenderToPng(BuildStrokeSvg(dxfPath), width);

    /// <summary>As <see cref="RenderToPng(string,int)"/> but from in-memory DXF text.</summary>
    public static byte[] RenderToPngFromContent(string dxfContent, int width)
        => SvgRasterizer.RenderToPng(BuildStrokeSvgFromContent(dxfContent), width);

    /// <summary>Builds a stroke-only SVG showing the DXF file's flattened geometry.</summary>
    public static string BuildStrokeSvg(string dxfPath)
        => BuildStrokeSvgFromEntities(DxfParser.ParseEntities(dxfPath));

    /// <summary>As <see cref="BuildStrokeSvg"/> but from in-memory DXF text.</summary>
    public static string BuildStrokeSvgFromContent(string dxfContent)
        => BuildStrokeSvgFromEntities(DxfParser.ParseEntitiesFromText(dxfContent));

    /// <summary>Builds a stroke-only SVG showing the flattened geometry. The orientation matches
    /// <see cref="SvgWriter"/> (both axes flipped) so the DXF preview lines up with the SVG the
    /// converter later produces.</summary>
    private static string BuildStrokeSvgFromEntities(List<IEntity> entities)
    {
        List<Segment> segments = Geometry.FlattenEntities(entities);
        if (segments.Count == 0)
        {
            throw new InvalidOperationException("DXF has no drawable geometry");
        }

        var points = new List<Point>(segments.Count * 2);
        foreach (Segment s in segments)
        {
            points.Add(s.Start);
            points.Add(s.End);
        }
        Bounds bounds = Geometry.BoundsForPoints(points);
        // Guard against a zero-size axis (e.g. a single horizontal line) so the viewBox is valid.
        double w = bounds.Width > 0 ? bounds.Width : 1;
        double h = bounds.Height > 0 ? bounds.Height : 1;
        double strokeWidth = Math.Max(w, h) / 300.0;

        var body = new StringBuilder();
        foreach (Segment s in segments)
        {
            Point a = Transform(s.Start, bounds);
            Point b = Transform(s.End, bounds);
            body.Append(
                $"    <line x1=\"{Fmt(a.X)}\" y1=\"{Fmt(a.Y)}\" x2=\"{Fmt(b.X)}\" y2=\"{Fmt(b.Y)}\" />\n");
        }

        return
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {Fmt(w)} {Fmt(h)}\" " +
            $"width=\"{Fmt(w)}\" height=\"{Fmt(h)}\">\n" +
            $"  <g fill=\"none\" stroke=\"#111827\" stroke-width=\"{Fmt(strokeWidth)}\" " +
            "stroke-linecap=\"round\">\n" +
            body +
            "  </g>\n" +
            "</svg>\n";
    }

    /// <summary>Mirrors <c>SvgWriter.Transform</c>: flips BOTH X and Y (180-degree rotation).</summary>
    private static Point Transform(Point p, Bounds bounds)
        => new(bounds.MaxX - p.X, bounds.MaxY - p.Y);

    private static string Fmt(double value)
    {
        string text = value.ToString("F6", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
        return text.Length > 0 ? text : "0";
    }
}
