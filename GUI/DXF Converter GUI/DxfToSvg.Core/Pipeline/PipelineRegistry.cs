using DxfToSvg.Core.Pipeline.Transitions;

namespace DxfToSvg.Core.Pipeline;

/// <summary>The hub of the "anything → anything" pipeline: a registry of available transitions and
/// per-format preview renderers. Callers discover what a given artifact can become
/// (<see cref="TransitionsFrom"/>), resolve a specific conversion (<see cref="Find"/> /
/// <see cref="FindById"/>), and render previews (<see cref="RenderPreview"/>) — all without knowing
/// the concrete transition types. Adding a format or conversion is a registration, not a rewrite.</summary>
public sealed class PipelineRegistry
{
    private readonly List<ITransition> _transitions = new();
    private readonly Dictionary<string, IPreviewRenderer> _previewsByFormatId = new();

    public IReadOnlyList<ITransition> Transitions => _transitions;

    public void Register(ITransition transition) => _transitions.Add(transition);

    public void Register(IPreviewRenderer renderer) => _previewsByFormatId[renderer.Format.Id] = renderer;

    /// <summary>All transitions whose input is <paramref name="from"/> — i.e. everything this
    /// format can be converted into (including same-format transforms).</summary>
    public IEnumerable<ITransition> TransitionsFrom(Format from)
        => _transitions.Where(t => t.From.Equals(from));

    /// <summary>All formats reachable in one step from <paramref name="from"/>.</summary>
    public IEnumerable<Format> TargetsFrom(Format from)
        => TransitionsFrom(from).Select(t => t.To).Distinct();

    public ITransition? Find(Format from, Format to)
        => _transitions.FirstOrDefault(t => t.From.Equals(from) && t.To.Equals(to));

    public ITransition? FindById(string id)
        => _transitions.FirstOrDefault(t => t.Id == id);

    /// <summary>Renders a preview of <paramref name="artifact"/>, or null if its format has no
    /// registered preview renderer.</summary>
    public byte[]? RenderPreview(Artifact artifact, int width)
        => _previewsByFormatId.TryGetValue(artifact.Format.Id, out IPreviewRenderer? renderer)
            ? renderer.RenderPng(artifact, width)
            : null;

    /// <summary>The default registry this app ships with: DXF/SVG/PNG previews and the
    /// DXF→SVG, SVG→PNG, and rotate (SVG→SVG, PNG→PNG) transitions.</summary>
    public static PipelineRegistry CreateDefault()
    {
        var registry = new PipelineRegistry();

        registry.Register(new DxfToSvgTransition());
        registry.Register(new SvgToPngTransition());
        registry.Register(new RotateSvgClockwiseTransition());
        registry.Register(new RotatePngClockwiseTransition());

        registry.Register(new DxfPreview());
        registry.Register(new SvgPreview());
        registry.Register(new PngPreview());

        return registry;
    }
}
