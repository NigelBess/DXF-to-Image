namespace DxfToSvg.Core.Pipeline.Transitions;

/// <summary>Rasterizes an SVG to a PNG at a chosen width (height follows the aspect ratio).
/// Wraps <see cref="SvgRasterizer"/> behind the <see cref="ITransition"/> seam.</summary>
public sealed class SvgToPngTransition : Transition
{
    public const string WidthKey = "width";

    public override string Id => "svg->png";
    public override string Name => "SVG → PNG";
    public override Format From => Format.Svg;
    public override Format To => Format.Png;

    public override IReadOnlyList<TransitionParameter> Parameters { get; } = new[]
    {
        new TransitionParameter(WidthKey, "Width (px)", ParameterKind.Integer, 813, Min: 1, Max: 20000),
    };

    public override Artifact Apply(Artifact input, IReadOnlyDictionary<string, object?> arguments)
    {
        int width = GetInt(arguments, WidthKey);
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(arguments), "Width must be a positive number.");
        }
        byte[] png = SvgRasterizer.RenderToPng(input.AsText(), width);
        return Artifact.FromBytes(Format.Png, png);
    }
}
