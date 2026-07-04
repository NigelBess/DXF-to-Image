namespace DxfToSvg.Core;

public readonly record struct Bounds(double MinX, double MinY, double MaxX, double MaxY)
{
    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;
}

/// <summary>A flattened line segment (a pair of points).</summary>
public readonly record struct Segment(Point Start, Point End);

/// <summary>Port of <c>dxf_to_svg/geometry.py</c>: curve flattening, area, bounds.</summary>
public static class Geometry
{
    public static Point PointOnCircle(Point center, double radius, double angleDegrees)
    {
        double radians = angleDegrees * Math.PI / 180.0;
        return new Point(
            center.X + radius * Math.Cos(radians),
            center.Y + radius * Math.Sin(radians));
    }

    public static List<Point> FlattenArc(Arc arc, double maxStepDegrees = 4.0)
    {
        double start = arc.StartAngle;
        double end = arc.EndAngle;
        while (end <= start)
        {
            end += 360.0;
        }
        double sweep = end - start;
        int steps = Math.Max(2, (int)Math.Ceiling(sweep / maxStepDegrees));
        var points = new List<Point>(steps + 1);
        for (int index = 0; index <= steps; index++)
        {
            points.Add(PointOnCircle(arc.Center, arc.Radius, start + sweep * index / steps));
        }
        return points;
    }

    public static List<Point> FlattenCircle(Circle circle, double maxStepDegrees = 4.0)
    {
        int steps = Math.Max(24, (int)Math.Ceiling(360.0 / maxStepDegrees));
        var points = new List<Point>(steps + 1);
        for (int index = 0; index <= steps; index++)
        {
            points.Add(PointOnCircle(circle.Center, circle.Radius, 360.0 * index / steps));
        }
        return points;
    }

    /// <summary>Turns each entity into line segments. Note the default step of 3.0 degrees,
    /// which overrides the 4.0 defaults on the individual flatten functions (matches Python).</summary>
    public static List<Segment> FlattenEntities(IEnumerable<IEntity> entities, double maxStepDegrees = 3.0)
    {
        var segments = new List<Segment>();
        foreach (IEntity entity in entities)
        {
            switch (entity)
            {
                case Line line:
                    segments.Add(new Segment(line.Start, line.End));
                    break;
                case Arc arc:
                    AddPolyline(segments, FlattenArc(arc, maxStepDegrees));
                    break;
                case Circle circle:
                    AddPolyline(segments, FlattenCircle(circle, maxStepDegrees));
                    break;
            }
        }
        return segments;
    }

    private static void AddPolyline(List<Segment> segments, List<Point> points)
    {
        for (int i = 0; i + 1 < points.Count; i++)
        {
            segments.Add(new Segment(points[i], points[i + 1]));
        }
    }

    /// <summary>Signed area via the shoelace formula, divided by 2.</summary>
    public static double PolygonArea(IReadOnlyList<Point> points)
    {
        if (points.Count < 3)
        {
            return 0.0;
        }
        double total = 0.0;
        for (int i = 0; i < points.Count; i++)
        {
            Point current = points[i];
            Point next = points[(i + 1) % points.Count];
            total += current.X * next.Y - next.X * current.Y;
        }
        return total / 2.0;
    }

    public static Bounds BoundsForPoints(IReadOnlyList<Point> points)
    {
        if (points.Count == 0)
        {
            throw new ArgumentException("Cannot compute bounds for empty point set");
        }
        double minX = double.PositiveInfinity, minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;
        foreach (Point p in points)
        {
            minX = Math.Min(minX, p.X);
            minY = Math.Min(minY, p.Y);
            maxX = Math.Max(maxX, p.X);
            maxY = Math.Max(maxY, p.Y);
        }
        return new Bounds(minX, minY, maxX, maxY);
    }

    public static double Distance(Point a, Point b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
