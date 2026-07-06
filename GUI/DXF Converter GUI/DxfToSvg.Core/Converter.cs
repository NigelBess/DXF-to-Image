namespace DxfToSvg.Core;

/// <summary>Port of <c>dxf_to_svg/convert.py</c>: the DXF -> SVG pipeline orchestration.</summary>
public static class Converter
{
    public const string DefaultFill = "#6b3f22";

    /// <summary>Runs parse -> flatten -> trace -> emit and returns the SVG plus face count.</summary>
    public static (string Svg, int FaceCount) ConvertToSvg(
        string dxfPath,
        string fill = DefaultFill,
        bool debugStrokes = false,
        double epsilon = 1e-5)
        => ConvertEntitiesToSvg(DxfParser.ParseEntities(dxfPath), fill, debugStrokes, epsilon);

    /// <summary>Same as <see cref="ConvertToSvg(string,string,bool,double)"/> but from in-memory
    /// DXF text rather than a file path.</summary>
    public static (string Svg, int FaceCount) ConvertToSvgFromContent(
        string dxfContent,
        string fill = DefaultFill,
        bool debugStrokes = false,
        double epsilon = 1e-5)
        => ConvertEntitiesToSvg(DxfParser.ParseEntitiesFromText(dxfContent), fill, debugStrokes, epsilon);

    private static (string Svg, int FaceCount) ConvertEntitiesToSvg(
        List<IEntity> entities, string fill, bool debugStrokes, double epsilon)
    {
        List<Segment> segments = Geometry.FlattenEntities(entities);
        List<List<Point>> faces = FaceTracer.TraceFaces(segments, epsilon);
        var faceList = faces.Cast<IReadOnlyList<Point>>().ToList();
        string svg = SvgWriter.SvgDocument(faceList, fill, debugStrokes);
        return (svg, faces.Count);
    }

    /// <summary>Converts a DXF file to an SVG file. Returns the number of filled faces.</summary>
    public static int ConvertFile(
        string dxfPath,
        string outputPath,
        string fill = DefaultFill,
        bool debugStrokes = false,
        double epsilon = 1e-5)
    {
        (string svg, int faceCount) = ConvertToSvg(dxfPath, fill, debugStrokes, epsilon);
        File.WriteAllText(outputPath, svg);
        return faceCount;
    }
}
