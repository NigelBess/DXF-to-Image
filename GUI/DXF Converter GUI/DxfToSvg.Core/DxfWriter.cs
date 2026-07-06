using System.Globalization;
using System.Text;

namespace DxfToSvg.Core;

/// <summary>Emits a minimal DXF (an ENTITIES section of LINE / ARC / CIRCLE) from parsed entities.
/// This is the inverse of the subset <see cref="DxfParser"/> reads — it round-trips exactly the
/// entity kinds this app understands, so a re-emitted DXF is consistent with the pipeline. It does
/// not preserve any DXF content beyond those entities.</summary>
public static class DxfWriter
{
    public static string Write(IEnumerable<IEntity> entities)
    {
        var sb = new StringBuilder();
        Pair(sb, 0, "SECTION");
        Pair(sb, 2, "ENTITIES");
        foreach (IEntity entity in entities)
        {
            WriteEntity(sb, entity);
        }
        Pair(sb, 0, "ENDSEC");
        Pair(sb, 0, "EOF");
        return sb.ToString();
    }

    private static void WriteEntity(StringBuilder sb, IEntity entity)
    {
        switch (entity)
        {
            case Line line:
                Pair(sb, 0, "LINE");
                Pair(sb, 8, "0");
                Point(sb, 10, 20, 30, line.Start);
                Point(sb, 11, 21, 31, line.End);
                break;
            case Arc arc:
                Pair(sb, 0, "ARC");
                Pair(sb, 8, "0");
                Point(sb, 10, 20, 30, arc.Center);
                Pair(sb, 40, Num(arc.Radius));
                Pair(sb, 50, Num(arc.StartAngle));
                Pair(sb, 51, Num(arc.EndAngle));
                break;
            case Circle circle:
                Pair(sb, 0, "CIRCLE");
                Pair(sb, 8, "0");
                Point(sb, 10, 20, 30, circle.Center);
                Pair(sb, 40, Num(circle.Radius));
                break;
        }
    }

    private static void Point(StringBuilder sb, int xCode, int yCode, int zCode, Point p)
    {
        Pair(sb, xCode, Num(p.X));
        Pair(sb, yCode, Num(p.Y));
        Pair(sb, zCode, "0.0");
    }

    private static void Pair(StringBuilder sb, int code, string value)
    {
        sb.Append(code.ToString(CultureInfo.InvariantCulture)).Append('\n');
        sb.Append(value).Append('\n');
    }

    private static string Num(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
