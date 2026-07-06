using DxfToSvg.Core;
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

    [Fact]
    public void EveryImageFormatHasARotateTransition()
    {
        // The UI shows Rotate on every preview; each format must expose a same-format transition.
        Assert.NotNull(Registry.FindById(RotateDxfClockwiseTransition.TransitionId));
        Assert.NotNull(Registry.FindById(RotateSvgClockwiseTransition.TransitionId));
        Assert.NotNull(Registry.FindById(RotatePngClockwiseTransition.TransitionId));
        Assert.Contains(Format.Dxf, Registry.TargetsFrom(Format.Dxf)); // DXF → DXF is discoverable
    }

    [Fact]
    public void RotatingDxfMapsPointsNinetyDegrees()
    {
        // A horizontal line (0,0)→(10,0) rotates to (0,0)→(0,10) under (x,y) → (-y,x).
        string dxf = DxfWriter.Write(new IEntity[] { new Line(new Point(0, 0), new Point(10, 0)) });
        ITransition rotate = Registry.FindById(RotateDxfClockwiseTransition.TransitionId)!;

        Artifact rotated = rotate.Apply(Artifact.FromText(Format.Dxf, dxf), new Dictionary<string, object?>());
        Line line = Assert.IsType<Line>(Assert.Single(DxfParser.ParseEntitiesFromText(rotated.AsText())));

        Assert.Equal(0, line.Start.X, 6);
        Assert.Equal(0, line.Start.Y, 6);
        Assert.Equal(0, line.End.X, 6);
        Assert.Equal(10, line.End.Y, 6);
    }

    [Fact]
    public void RotatingTheDxfMatchesRotatingTheConvertedSvg()
    {
        // Rotating the DXF then converting must look the same as converting then rotating the SVG —
        // i.e. the DXF rotate turns the drawing the same visual direction as the SVG/PNG rotate.
        ITransition dxfToSvg = Registry.Find(Format.Dxf, Format.Svg)!;
        ITransition rotateDxf = Registry.FindById(RotateDxfClockwiseTransition.TransitionId)!;
        ITransition rotateSvg = Registry.FindById(RotateSvgClockwiseTransition.TransitionId)!;
        var noArgs = new Dictionary<string, object?>();

        Artifact svg = dxfToSvg.Apply(DxfArtifact(), noArgs);
        Artifact viaSvgRotate = rotateSvg.Apply(svg, noArgs);
        Artifact viaDxfRotate = dxfToSvg.Apply(rotateDxf.Apply(DxfArtifact(), noArgs), noArgs);

        double iou = MaskIoU(
            SvgRasterizer.RenderToPng(viaSvgRotate.AsText(), 128),
            SvgRasterizer.RenderToPng(viaDxfRotate.AsText(), 128));
        Assert.True(iou > 0.9, $"orientation mismatch between DXF-rotate and SVG-rotate (IoU={iou:0.###})");
    }

    /// <summary>Intersection-over-union of the opaque (rendered) pixels of two equally sized PNGs.</summary>
    private static double MaskIoU(byte[] pngA, byte[] pngB)
    {
        using SKBitmap a = SKBitmap.Decode(pngA);
        using SKBitmap b = SKBitmap.Decode(pngB);
        Assert.Equal(a.Width, b.Width);
        Assert.Equal(a.Height, b.Height);

        int intersection = 0, union = 0;
        for (int y = 0; y < a.Height; y++)
        {
            for (int x = 0; x < a.Width; x++)
            {
                bool inA = a.GetPixel(x, y).Alpha > 8;
                bool inB = b.GetPixel(x, y).Alpha > 8;
                if (inA && inB) intersection++;
                if (inA || inB) union++;
            }
        }
        return union == 0 ? 1.0 : (double)intersection / union;
    }
}
