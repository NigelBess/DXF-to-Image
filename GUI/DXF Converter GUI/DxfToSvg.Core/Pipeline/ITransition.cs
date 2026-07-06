namespace DxfToSvg.Core.Pipeline;

/// <summary>A conversion from one <see cref="Format"/> to another. The endpoints may be the same
/// format (e.g. a rotate is SVG → SVG), which is what lets a future editor build pipelines from
/// anything to anything. Implementations are fully self-describing — their <see cref="From"/>/
/// <see cref="To"/> formats and their <see cref="Parameters"/> schema — so the pipeline can be
/// discovered and driven generically without the caller knowing the concrete type.</summary>
public interface ITransition
{
    /// <summary>Stable unique identifier (e.g. "dxf->svg").</summary>
    string Id { get; }

    /// <summary>Human-facing name (e.g. "DXF → SVG").</summary>
    string Name { get; }

    Format From { get; }
    Format To { get; }

    /// <summary>The inputs this transition accepts, for generic UI generation. May be empty.</summary>
    IReadOnlyList<TransitionParameter> Parameters { get; }

    /// <summary>Runs the conversion. <paramref name="arguments"/> is keyed by
    /// <see cref="TransitionParameter.Key"/>; missing keys fall back to declared defaults.</summary>
    Artifact Apply(Artifact input, IReadOnlyDictionary<string, object?> arguments);
}
