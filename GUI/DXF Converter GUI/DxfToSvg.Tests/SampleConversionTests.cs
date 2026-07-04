using DxfToSvg.Core;

namespace DxfToSvg.Tests;

/// <summary>Port of <c>tests/test_sample.py</c> — the same acceptance test plan:
/// entity counts on the sample, and a tolerant visual (IoU) match against the reference PNG.</summary>
public class SampleConversionTests
{
    private static readonly string FixturesDir = Path.Combine(AppContext.BaseDirectory, "Fixtures");
    private static readonly string SampleDxf = Path.Combine(FixturesDir, "Nebula Logo.dxf");
    private static readonly string ReferencePng = Path.Combine(FixturesDir, "logo.png");

    [Fact]
    public void ParsesSampleEntities()
    {
        Dictionary<string, int> counts = DxfParser.EntityCounts(DxfParser.ParseEntities(SampleDxf));
        Assert.Equal(8, counts["LINE"]);
        Assert.Equal(11, counts["ARC"]);
    }

    [Fact]
    public void ConvertsSampleWithTolerantVisualMatch()
    {
        string outSvg = Path.Combine(Path.GetTempPath(), $"dxf-test-output-{Guid.NewGuid():N}.svg");
        try
        {
            int faceCount = Converter.ConvertFile(SampleDxf, outSvg, "#6b3f22");
            Assert.True(faceCount >= 1);
            Assert.True(File.Exists(outSvg));

            double score = MaskComparer.Compare(outSvg, ReferencePng, toleranceRadius: 3);
            Assert.True(score >= 0.95, $"IoU was {score:F4}, expected >= 0.95");
        }
        finally
        {
            if (File.Exists(outSvg))
            {
                File.Delete(outSvg);
            }
        }
    }
}
