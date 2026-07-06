using DxfToSvg.Core.Pipeline;
using DxfToSvg.Core.Pipeline.Transitions;
using SkiaSharp;

namespace DxfToSvg.Tests;

/// <summary>Verifies the format/artifact/transition abstraction: transitions are discoverable and
/// resolvable through the registry, same-format (A→A) transitions work, and artifacts chain from
/// one format through another ("anything → anything").</summary>
public class PipelineTests
{
    private static readonly string SampleDxf =
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Nebula Logo.dxf");

    private static Artifact DxfArtifact()
        => Artifact.FromBytes(Format.Dxf, File.ReadAllBytes(SampleDxf));

    private static readonly PipelineRegistry Registry = PipelineRegistry.CreateDefault();

    [Fact]
    public void RegistryResolvesTransitionsByEndpointsAndId()
    {
        Assert.IsType<DxfToSvgTransition>(Registry.Find(Format.Dxf, Format.Svg));
        Assert.IsType<SvgToPngTransition>(Registry.Find(Format.Svg, Format.Png));
        Assert.NotNull(Registry.FindById(RotateSvgClockwiseTransition.TransitionId));
        Assert.NotNull(Registry.FindById(RotatePngClockwiseTransition.TransitionId));
        Assert.Null(Registry.Find(Format.Png, Format.Dxf)); // not registered
    }

    [Fact]
    public void DiscoveryListsEverythingAFormatCanBecome()
    {
        var dxfTargets = Registry.TargetsFrom(Format.Dxf).ToList();
        Assert.Contains(Format.Svg, dxfTargets);

        // SVG can become PNG (format change) and SVG (rotate) — proving A→A is discoverable.
        var svgTargets = Registry.TargetsFrom(Format.Svg).ToList();
        Assert.Contains(Format.Png, svgTargets);
        Assert.Contains(Format.Svg, svgTargets);
    }

    [Fact]
    public void SameFormatRotateTransitionKeepsFormat()
    {
        ITransition rotate = Registry.FindById(RotateSvgClockwiseTransition.TransitionId)!;
        Assert.Equal(Format.Svg, rotate.From);
        Assert.Equal(Format.Svg, rotate.To);
    }

    [Fact]
    public void ChainsDxfThroughSvgToPng()
    {
        ITransition toSvg = Registry.Find(Format.Dxf, Format.Svg)!;
        ITransition toPng = Registry.Find(Format.Svg, Format.Png)!;

        // Omit the fill arg entirely — the transition must fall back to its declared default.
        Artifact svg = toSvg.Apply(DxfArtifact(), new Dictionary<string, object?>());
        Assert.Equal(Format.Svg, svg.Format);
        Assert.Contains("<svg", svg.AsText());

        Artifact png = toPng.Apply(svg, new Dictionary<string, object?> { ["width"] = 300 });
        Assert.Equal(Format.Png, png.Format);

        using SKBitmap bitmap = SKBitmap.Decode(png.Data);
        Assert.Equal(300, bitmap.Width);
        Assert.True(bitmap.Height > 0);
    }

    [Fact]
    public void TransitionParameterSchemaIsExposedForUiGeneration()
    {
        ITransition toSvg = Registry.Find(Format.Dxf, Format.Svg)!;
        TransitionParameter fill = Assert.Single(toSvg.Parameters);
        Assert.Equal(DxfToSvgTransition.FillKey, fill.Key);
        Assert.Equal(ParameterKind.Color, fill.Kind);

        ITransition toPng = Registry.Find(Format.Svg, Format.Png)!;
        TransitionParameter width = Assert.Single(toPng.Parameters);
        Assert.Equal(ParameterKind.Integer, width.Kind);
    }

    [Fact]
    public void RegistryRendersPreviewPerFormat()
    {
        Artifact dxf = DxfArtifact();
        byte[]? dxfPreview = Registry.RenderPreview(dxf, 200);
        Assert.NotNull(dxfPreview);
        using SKBitmap bitmap = SKBitmap.Decode(dxfPreview);
        Assert.Equal(200, bitmap.Width);
    }
}
