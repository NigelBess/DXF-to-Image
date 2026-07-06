namespace DxfToSvg.Core.Pipeline.Transitions;

/// <summary>Traces the enclosed regions of a DXF and fills them, producing an SVG.
/// Wraps <see cref="Converter"/> behind the <see cref="ITransition"/> seam so it can be swapped
/// for an alternative DXF → SVG implementation without touching callers.</summary>
public sealed class DxfToSvgTransition : Transition
{
    public const string FillKey = "fill";

    public override string Id => "dxf->svg";
    public override string Name => "DXF → SVG";
    public override Format From => Format.Dxf;
    public override Format To => Format.Svg;

    public override IReadOnlyList<TransitionParameter> Parameters { get; } = new[]
    {
        new TransitionParameter(FillKey, "Fill color", ParameterKind.Color, Converter.DefaultFill),
    };

    public override Artifact Apply(Artifact input, IReadOnlyDictionary<string, object?> arguments)
    {
        string fill = GetString(arguments, FillKey);
        if (string.IsNullOrWhiteSpace(fill))
        {
            fill = Converter.DefaultFill;
        }
        (string svg, _) = Converter.ConvertToSvgFromContent(input.AsText(), fill);
        return Artifact.FromText(Format.Svg, svg);
    }
}
