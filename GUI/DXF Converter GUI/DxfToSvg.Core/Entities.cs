namespace DxfToSvg.Core;

/// <summary>2D point. A struct with value equality so snapped points can key the
/// face-tracing adjacency dictionary (mirrors Python's tuple equality).</summary>
public readonly record struct Point(double X, double Y);

/// <summary>A straight line entity. Ports the Python <c>Line</c> dataclass.</summary>
public sealed record Line(Point Start, Point End) : IEntity;

/// <summary>A circular arc entity (angles in degrees, DXF CCW convention).</summary>
public sealed record Arc(Point Center, double Radius, double StartAngle, double EndAngle) : IEntity;

/// <summary>A full circle entity.</summary>
public sealed record Circle(Point Center, double Radius) : IEntity;

/// <summary>Marker for the supported DXF entity kinds (Line | Arc | Circle).</summary>
public interface IEntity
{
}
