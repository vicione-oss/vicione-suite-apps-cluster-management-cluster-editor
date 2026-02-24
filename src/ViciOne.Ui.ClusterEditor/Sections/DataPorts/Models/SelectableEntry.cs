namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

internal sealed record SelectableEntry(string Text, object? Value)
{
    public object? Value { get; set; } = Value;
}
