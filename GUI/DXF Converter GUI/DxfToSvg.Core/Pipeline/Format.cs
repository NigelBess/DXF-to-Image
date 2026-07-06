namespace DxfToSvg.Core.Pipeline;

/// <summary>An open, extensible description of a data format that can flow through the pipeline.
/// This is deliberately NOT an enum: new formats a future app supports are just new instances
/// (constructed anywhere and registered), so the set of formats is unbounded. Identity is the
/// <see cref="Id"/> alone, so two references to the same logical format compare equal and can key
/// dictionaries.</summary>
public sealed class Format : IEquatable<Format>
{
    /// <summary>Stable machine identifier (e.g. "dxf"). Used for equality and lookups.</summary>
    public string Id { get; }

    /// <summary>Human-facing name (e.g. "DXF").</summary>
    public string DisplayName { get; }

    /// <summary>Canonical file extension including the dot (e.g. ".dxf").</summary>
    public string Extension { get; }

    /// <summary>True when the payload is UTF-8 text (DXF, SVG), false when binary (PNG).</summary>
    public bool IsText { get; }

    public Format(string id, string displayName, string extension, bool isText)
    {
        Id = id;
        DisplayName = displayName;
        Extension = extension;
        IsText = isText;
    }

    // Well-known formats this app ships with. Additional formats need not live here.
    public static readonly Format Dxf = new("dxf", "DXF", ".dxf", isText: true);
    public static readonly Format Svg = new("svg", "SVG", ".svg", isText: true);
    public static readonly Format Png = new("png", "PNG", ".png", isText: false);

    public bool Equals(Format? other) => other is not null && Id == other.Id;
    public override bool Equals(object? obj) => Equals(obj as Format);
    public override int GetHashCode() => Id.GetHashCode();
    public override string ToString() => DisplayName;
}
