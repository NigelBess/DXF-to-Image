namespace DxfToSvg.Core.Pipeline.Transitions;

/// <summary>Rotates an SVG 90° clockwise. A same-format (SVG → SVG) transition — proof that the
/// abstraction handles A → A conversions, not just format changes.</summary>
public sealed class RotateSvgClockwiseTransition : Transition
{
    public const string TransitionId = "svg->svg:rotate-cw";

    public override string Id => TransitionId;
    public override string Name => "Rotate 90° CW";
    public override Format From => Format.Svg;
    public override Format To => Format.Svg;

    public override Artifact Apply(Artifact input, IReadOnlyDictionary<string, object?> arguments)
        => Artifact.FromText(Format.Svg, Transforms.RotateSvgClockwise90(input.AsText()));
}

/// <summary>Rotates a PNG 90° clockwise (PNG → PNG).</summary>
public sealed class RotatePngClockwiseTransition : Transition
{
    public const string TransitionId = "png->png:rotate-cw";

    public override string Id => TransitionId;
    public override string Name => "Rotate 90° CW";
    public override Format From => Format.Png;
    public override Format To => Format.Png;

    public override Artifact Apply(Artifact input, IReadOnlyDictionary<string, object?> arguments)
        => Artifact.FromBytes(Format.Png, Transforms.RotatePngClockwise90(input.Data));
}
