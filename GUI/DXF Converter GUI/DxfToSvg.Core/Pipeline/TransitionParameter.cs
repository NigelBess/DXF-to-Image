namespace DxfToSvg.Core.Pipeline;

/// <summary>The kind of editor a UI should render for a <see cref="TransitionParameter"/>.</summary>
public enum ParameterKind
{
    Text,
    Color,
    Integer,
}

/// <summary>Describes one user-adjustable input to a transition — enough metadata for a UI to
/// render an editor for it generically and for a headless caller to supply a sensible default.</summary>
public sealed record TransitionParameter(
    string Key,
    string DisplayName,
    ParameterKind Kind,
    object? DefaultValue,
    double? Min = null,
    double? Max = null);
