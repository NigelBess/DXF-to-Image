namespace DxfToSvg.Core.Pipeline.Transitions;

/// <summary>Rotates a DXF 90° clockwise (DXF → DXF) by transforming its LINE/ARC/CIRCLE geometry
/// and re-emitting a DXF via <see cref="DxfWriter"/>. Only the entity kinds the pipeline supports
/// survive the round-trip.
///
/// The point map is <c>(x, y) → (-y, x)</c> (a +90° rotation in DXF's Y-up space) with arc angles
/// shifted +90°. That looks counter-intuitive for a "clockwise" op, but <see cref="SvgWriter"/>
/// flips both axes when rendering, so this is what actually appears clockwise on screen — i.e. it
/// matches the SVG/PNG rotate transitions. See the pipeline tests, which assert this equivalence.</summary>
public sealed class RotateDxfClockwiseTransition : Transition
{
    public const string TransitionId = "dxf->dxf:rotate-cw";

    public override string Id => TransitionId;
    public override string Name => "Rotate 90° CW";
    public override Format From => Format.Dxf;
    public override Format To => Format.Dxf;

    public override Artifact Apply(Artifact input, IReadOnlyDictionary<string, object?> arguments)
    {
        var rotated = DxfParser.ParseEntitiesFromText(input.AsText()).Select(RotateClockwise90);
        return Artifact.FromText(Format.Dxf, DxfWriter.Write(rotated));
    }

    private static IEntity RotateClockwise90(IEntity entity) => entity switch
    {
        Line line => new Line(Rotate(line.Start), Rotate(line.End)),
        Arc arc => new Arc(Rotate(arc.Center), arc.Radius, arc.StartAngle + 90.0, arc.EndAngle + 90.0),
        Circle circle => new Circle(Rotate(circle.Center), circle.Radius),
        _ => entity,
    };

    private static Point Rotate(Point p) => new(-p.Y, p.X);
}
