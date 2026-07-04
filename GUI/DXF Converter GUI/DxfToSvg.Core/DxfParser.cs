using System.Globalization;

namespace DxfToSvg.Core;

/// <summary>Port of <c>dxf_to_svg/dxf.py</c>. Reads group-code/value pairs and
/// extracts LINE, ARC and CIRCLE entities from the ENTITIES section.</summary>
public static class DxfParser
{
    private static readonly HashSet<string> Supported = new() { "LINE", "ARC", "CIRCLE" };

    /// <summary>Reads the file as group-code/value line pairs (two lines at a time).</summary>
    public static List<(string Code, string Value)> ReadGroupPairs(string path)
    {
        string[] lines = File.ReadAllText(path).Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var pairs = new List<(string, string)>();
        for (int index = 0; index + 1 < lines.Length; index += 2)
        {
            pairs.Add((lines[index].Trim(), lines[index + 1].Trim()));
        }
        return pairs;
    }

    private static double Float(Dictionary<string, List<string>> values, string code, double? @default = null)
    {
        if (!values.TryGetValue(code, out List<string>? found) || found.Count == 0)
        {
            if (@default is null)
            {
                throw new InvalidDataException($"Missing required DXF group code {code}");
            }
            return @default.Value;
        }
        // Mirror Python's found[-1]: take the last occurrence.
        return double.Parse(found[^1], CultureInfo.InvariantCulture);
    }

    private static IEntity? ParseEntity(string kind, Dictionary<string, List<string>> values)
    {
        switch (kind)
        {
            case "LINE":
                return new Line(
                    new Point(Float(values, "10"), Float(values, "20")),
                    new Point(Float(values, "11"), Float(values, "21")));
            case "ARC":
                return new Arc(
                    new Point(Float(values, "10"), Float(values, "20")),
                    Float(values, "40"),
                    Float(values, "50"),
                    Float(values, "51"));
            case "CIRCLE":
                return new Circle(
                    new Point(Float(values, "10"), Float(values, "20")),
                    Float(values, "40"));
            default:
                return null;
        }
    }

    /// <summary>Parses the ENTITIES section into supported entities.</summary>
    public static List<IEntity> ParseEntities(string path)
    {
        List<(string Code, string Value)> pairs = ReadGroupPairs(path);
        var entities = new List<IEntity>();
        bool inEntities = false;
        string? pendingKind = null;
        var pendingValues = new Dictionary<string, List<string>>();

        void Flush()
        {
            if (pendingKind is not null)
            {
                IEntity? entity = ParseEntity(pendingKind, pendingValues);
                if (entity is not null)
                {
                    entities.Add(entity);
                }
            }
            pendingKind = null;
            pendingValues = new Dictionary<string, List<string>>();
        }

        foreach ((string code, string value) in pairs)
        {
            if (code == "0" && value == "SECTION")
            {
                continue;
            }
            if (code == "2" && value == "ENTITIES")
            {
                inEntities = true;
                continue;
            }
            if (!inEntities)
            {
                continue;
            }
            if (code == "0" && value == "ENDSEC")
            {
                Flush();
                break;
            }
            if (code == "0")
            {
                Flush();
                if (Supported.Contains(value))
                {
                    pendingKind = value;
                    pendingValues = new Dictionary<string, List<string>>();
                }
                continue;
            }
            if (pendingKind is not null)
            {
                if (!pendingValues.TryGetValue(code, out List<string>? list))
                {
                    list = new List<string>();
                    pendingValues[code] = list;
                }
                list.Add(value);
            }
        }

        return entities;
    }

    /// <summary>Counts entities keyed by upper-cased type name (LINE / ARC / CIRCLE).</summary>
    public static Dictionary<string, int> EntityCounts(IEnumerable<IEntity> entities)
    {
        var counts = new Dictionary<string, int>();
        foreach (IEntity entity in entities)
        {
            string name = entity.GetType().Name.ToUpperInvariant();
            counts[name] = counts.GetValueOrDefault(name) + 1;
        }
        return counts;
    }
}
