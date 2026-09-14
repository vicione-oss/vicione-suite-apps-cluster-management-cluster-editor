using ViciOne.Tree.Builder.Icons;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;

internal static class IconReferenceExtensions
{
    /// <summary>
    /// The name of the icon a node type or a ruleset root declares, or <see langword="null"/> when
    /// it declares none. Reads <see cref="IconReference.Id"/> rather than the <c>Icons</c> array
    /// that ViciOne.Tree.Builder 3.0.0 keeps only for backwards compatibility. A ruleset that
    /// declares no icon at all deserialises to a <see langword="null"/> reference.
    /// </summary>
    internal static string? GetName(this IconReference? icon)
        => string.IsNullOrWhiteSpace(icon?.Id) ? null : icon.Id;
}
