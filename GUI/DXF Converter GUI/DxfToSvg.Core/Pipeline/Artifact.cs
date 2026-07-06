using System.Text;

namespace DxfToSvg.Core.Pipeline;

/// <summary>A unit of data in a specific <see cref="Format"/>. The payload is always raw bytes so
/// that any format — textual (DXF, SVG) or binary (PNG) — is represented uniformly and can be
/// passed through a chain of <see cref="ITransition"/>s without special-casing.</summary>
public sealed class Artifact
{
    public Format Format { get; }
    public byte[] Data { get; }

    public Artifact(Format format, byte[] data)
    {
        Format = format;
        Data = data;
    }

    public static Artifact FromBytes(Format format, byte[] data) => new(format, data);

    public static Artifact FromText(Format format, string text) => new(format, Encoding.UTF8.GetBytes(text));

    /// <summary>Decodes the payload as UTF-8 text. Only meaningful for text formats.</summary>
    public string AsText() => Encoding.UTF8.GetString(Data);
}
