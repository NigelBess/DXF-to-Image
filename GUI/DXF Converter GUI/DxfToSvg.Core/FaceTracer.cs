namespace DxfToSvg.Core;

/// <summary>Port of <c>dxf_to_svg/faces.py</c>: snapping, intersection splitting,
/// and planar half-edge face tracing.</summary>
public static class FaceTracer
{
    private static Point SnapPoint(Point point, double epsilon)
        => new(Math.Round(point.X / epsilon) * epsilon, Math.Round(point.Y / epsilon) * epsilon);

    public static List<Segment> SnappedSegments(IEnumerable<Segment> segments, double epsilon = 1e-5)
    {
        var snapped = new List<Segment>();
        foreach (Segment segment in segments)
        {
            Point a = SnapPoint(segment.Start, epsilon);
            Point b = SnapPoint(segment.End, epsilon);
            if (!a.Equals(b))
            {
                snapped.Add(new Segment(a, b));
            }
        }
        return snapped;
    }

    private static double Cross(Point a, Point b) => a.X * b.Y - a.Y * b.X;

    private static Point Subtract(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);

    private static (double T, double U)? SegmentIntersection(Point a, Point b, Point c, Point d, double epsilon)
    {
        Point r = Subtract(b, a);
        Point s = Subtract(d, c);
        double denominator = Cross(r, s);
        if (Math.Abs(denominator) <= epsilon)
        {
            return null;
        }
        Point qp = Subtract(c, a);
        double t = Cross(qp, s) / denominator;
        double u = Cross(qp, r) / denominator;
        if (t >= -epsilon && t <= 1.0 + epsilon && u >= -epsilon && u <= 1.0 + epsilon)
        {
            return (Math.Min(1.0, Math.Max(0.0, t)), Math.Min(1.0, Math.Max(0.0, u)));
        }
        return null;
    }

    public static List<Segment> SplitSegmentsAtIntersections(IReadOnlyList<Segment> segments, double epsilon = 1e-5)
    {
        var cuts = new List<double>[segments.Count];
        for (int i = 0; i < segments.Count; i++)
        {
            cuts[i] = new List<double> { 0.0, 1.0 };
        }

        for (int i = 0; i < segments.Count; i++)
        {
            (Point a, Point b) = (segments[i].Start, segments[i].End);
            for (int j = i + 1; j < segments.Count; j++)
            {
                (Point c, Point d) = (segments[j].Start, segments[j].End);
                (double T, double U)? hit = SegmentIntersection(a, b, c, d, epsilon);
                if (hit is null)
                {
                    continue;
                }
                (double t, double u) = hit.Value;
                if (t > epsilon && t < 1.0 - epsilon)
                {
                    cuts[i].Add(t);
                }
                if (u > epsilon && u < 1.0 - epsilon)
                {
                    cuts[j].Add(u);
                }
            }
        }

        var split = new List<Segment>();
        for (int i = 0; i < segments.Count; i++)
        {
            (Point a, Point b) = (segments[i].Start, segments[i].End);
            var unique = cuts[i].Select(cut => Math.Round(cut, 10)).Distinct().OrderBy(cut => cut).ToList();
            for (int k = 0; k + 1 < unique.Count; k++)
            {
                double startT = unique[k];
                double endT = unique[k + 1];
                if (endT - startT <= epsilon)
                {
                    continue;
                }
                var start = new Point(a.X + (b.X - a.X) * startT, a.Y + (b.Y - a.Y) * startT);
                var end = new Point(a.X + (b.X - a.X) * endT, a.Y + (b.Y - a.Y) * endT);
                split.Add(new Segment(start, end));
            }
        }
        return split;
    }

    private static double Angle(Point a, Point b) => Math.Atan2(b.Y - a.Y, b.X - a.X);

    public static List<List<Point>> TraceFaces(IReadOnlyList<Segment> segments, double epsilon = 1e-5, double minArea = 1e-4)
    {
        List<Segment> snapped = SnappedSegments(SplitSegmentsAtIntersections(segments, epsilon), epsilon);

        // Preserve first-seen vertex order (mirrors Python dict insertion order),
        // which the traversal below depends on for deterministic results.
        var order = new List<Point>();
        var adjacency = new Dictionary<Point, HashSet<Point>>();

        void Link(Point from, Point to)
        {
            if (!adjacency.TryGetValue(from, out HashSet<Point>? set))
            {
                set = new HashSet<Point>();
                adjacency[from] = set;
                order.Add(from);
            }
            set.Add(to);
        }

        foreach (Segment segment in snapped)
        {
            Link(segment.Start, segment.End);
            Link(segment.End, segment.Start);
        }

        var ordered = new Dictionary<Point, List<Point>>();
        foreach (Point vertex in order)
        {
            ordered[vertex] = adjacency[vertex].OrderBy(n => Angle(vertex, n)).ToList();
        }

        var visited = new HashSet<(Point, Point)>();
        var faces = new List<List<Point>>();

        foreach (Point start in order)
        {
            foreach (Point nxt in ordered[start])
            {
                if (visited.Contains((start, nxt)))
                {
                    continue;
                }
                var face = new List<Point>();
                Point current = start;
                Point target = nxt;
                int limit = snapped.Count * 4 + 10;
                for (int step = 0; step < limit; step++)
                {
                    if (visited.Contains((current, target)))
                    {
                        break;
                    }
                    visited.Add((current, target));
                    face.Add(current);
                    List<Point> neighbors = ordered[target];
                    int reverseIndex = neighbors.IndexOf(current);
                    int n = neighbors.Count;
                    int nextIndex = ((reverseIndex - 1) % n + n) % n;
                    current = target;
                    target = neighbors[nextIndex];
                    if (current.Equals(start) && target.Equals(nxt))
                    {
                        break;
                    }
                }
                if (face.Count >= 3)
                {
                    double area = Geometry.PolygonArea(face);
                    if (Math.Abs(area) >= minArea)
                    {
                        faces.Add(face);
                    }
                }
            }
        }

        if (faces.Count == 0)
        {
            return new List<List<Point>>();
        }

        // The unbounded exterior is a large negative loop; positive loops are filled regions.
        var positive = faces.Where(face => Geometry.PolygonArea(face) > minArea).ToList();
        if (positive.Count > 0)
        {
            return positive.OrderByDescending(face => Math.Abs(Geometry.PolygonArea(face))).ToList();
        }

        return faces.OrderByDescending(face => Math.Abs(Geometry.PolygonArea(face))).Skip(1).ToList();
    }
}
