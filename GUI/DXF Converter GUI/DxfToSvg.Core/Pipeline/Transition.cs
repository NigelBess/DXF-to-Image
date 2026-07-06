namespace DxfToSvg.Core.Pipeline;

/// <summary>Convenience base for <see cref="ITransition"/> implementations. Supplies an empty
/// parameter list by default and typed argument lookups that fall back to each parameter's
/// declared default when the caller omits it or passes a null/invalid value.</summary>
public abstract class Transition : ITransition
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract Format From { get; }
    public abstract Format To { get; }

    public virtual IReadOnlyList<TransitionParameter> Parameters => Array.Empty<TransitionParameter>();

    public abstract Artifact Apply(Artifact input, IReadOnlyDictionary<string, object?> arguments);

    protected string GetString(IReadOnlyDictionary<string, object?> arguments, string key)
    {
        if (arguments.TryGetValue(key, out object? value) && value is not null)
        {
            return value.ToString() ?? string.Empty;
        }
        return DefaultOf(key)?.ToString() ?? string.Empty;
    }

    protected int GetInt(IReadOnlyDictionary<string, object?> arguments, string key)
    {
        if (arguments.TryGetValue(key, out object? value) && value is not null)
        {
            if (value is int i)
            {
                return i;
            }
            if (int.TryParse(value.ToString(), out int parsed))
            {
                return parsed;
            }
        }
        return DefaultOf(key) is int d ? d : 0;
    }

    private object? DefaultOf(string key)
    {
        foreach (TransitionParameter parameter in Parameters)
        {
            if (parameter.Key == key)
            {
                return parameter.DefaultValue;
            }
        }
        return null;
    }
}
