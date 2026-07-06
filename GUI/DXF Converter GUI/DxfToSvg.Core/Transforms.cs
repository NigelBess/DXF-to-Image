using System.Globalization;
using System.Xml.Linq;
using SkiaSharp;

namespace DxfToSvg.Core;

/// <summary>Rotates pipeline outputs 90 degrees clockwise. SVGs are rotated by wrapping
/// their content in a transform group and swapping the viewBox dimensions (lossless, works
/// on arbitrary uploaded SVGs); PNGs are rotated pixel-for-pixel via SkiaSharp.</summary>
public static class Transforms
{
    private static readonly XNamespace SvgNs = "http://www.w3.org/2000/svg";

    /// <summary>Rotates an SVG document 90 degrees clockwise. All top-level content is wrapped
    /// in a group with a rotation transform and the viewBox/width/height are swapped so the
    /// rotated drawing stays fully visible.</summary>
    public static string RotateSvgClockwise90(string svgContent)
    {
        XDocument doc = XDocument.Parse(svgContent, LoadOptions.PreserveWhitespace);
        XElement root = doc.Root ?? throw new InvalidDataException("SVG has no root element");

        (double vx, double vy, double vw, double vh) = ReadViewBox(root);

        // 90 deg CW (SVG y-down): original (x,y) -> (vh + vy - y, x - vx).
        // As matrix(a b c d e f): a=0 b=1 c=-1 d=0 e=vh+vy f=-vx.
        string transform = string.Format(
            CultureInfo.InvariantCulture,
            "matrix(0,1,-1,0,{0},{1})",
            Fmt(vh + vy),
            Fmt(-vx));

        var group = new XElement(SvgNs + "g", new XAttribute("transform", transform));
        // Move every child node (paths, groups, defs, whitespace) into the rotation group.
        var children = root.Nodes().ToList();
        foreach (XNode child in children)
        {
            child.Remove();
        }
        group.Add(children);
        root.Add(group);

        // Swap dimensions: rotated drawing is vh wide and vw tall.
        root.SetAttributeValue("viewBox", string.Format(
            CultureInfo.InvariantCulture, "0 0 {0} {1}", Fmt(vh), Fmt(vw)));
        if (root.Attribute("width") is not null)
        {
            root.SetAttributeValue("width", Fmt(vh));
        }
        if (root.Attribute("height") is not null)
        {
            root.SetAttributeValue("height", Fmt(vw));
        }

        return doc.Declaration is not null
            ? doc.Declaration + "\n" + doc.ToString(SaveOptions.DisableFormatting) + "\n"
            : doc.ToString(SaveOptions.DisableFormatting) + "\n";
    }

    /// <summary>Rotates PNG bytes 90 degrees clockwise, returning new PNG bytes.</summary>
    public static byte[] RotatePngClockwise90(byte[] png)
    {
        using SKBitmap source = SKBitmap.Decode(png)
            ?? throw new InvalidDataException("Could not decode PNG");
        using var rotated = new SKBitmap(source.Height, source.Width, source.ColorType, source.AlphaType);
        using (var canvas = new SKCanvas(rotated))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.Translate(rotated.Width, 0);
            canvas.RotateDegrees(90);
            canvas.DrawBitmap(source, 0, 0);
        }
        using SKImage image = SKImage.FromBitmap(rotated);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>Reads the drawing bounds, preferring the viewBox and falling back to
    /// width/height (treated as a viewBox anchored at the origin).</summary>
    private static (double X, double Y, double W, double H) ReadViewBox(XElement root)
    {
        string? viewBox = root.Attribute("viewBox")?.Value;
        if (!string.IsNullOrWhiteSpace(viewBox))
        {
            double[] parts = viewBox
                .Split(new[] { ' ', ',', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseLength)
                .ToArray();
            if (parts.Length == 4 && parts[2] > 0 && parts[3] > 0)
            {
                return (parts[0], parts[1], parts[2], parts[3]);
            }
        }

        double w = ParseLength(root.Attribute("width")?.Value);
        double h = ParseLength(root.Attribute("height")?.Value);
        if (w > 0 && h > 0)
        {
            return (0, 0, w, h);
        }

        throw new InvalidDataException("SVG has no usable viewBox or width/height to rotate");
    }

    /// <summary>Parses a length, ignoring a trailing unit like <c>px</c>.</summary>
    private static double ParseLength(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }
        int end = 0;
        while (end < value.Length && (char.IsDigit(value[end]) || value[end] is '.' or '-' or '+' or 'e' or 'E'))
        {
            end++;
        }
        return double.TryParse(value.AsSpan(0, end), NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
            ? result
            : 0;
    }

    private static string Fmt(double value)
    {
        string text = value.ToString("F6", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
        return text.Length > 0 ? text : "0";
    }
}
