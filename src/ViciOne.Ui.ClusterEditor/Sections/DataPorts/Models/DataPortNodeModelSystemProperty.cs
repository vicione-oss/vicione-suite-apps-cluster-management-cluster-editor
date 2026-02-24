using System.Collections.Generic;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

internal abstract class DataPortNodeModelSystemProperty : IDataPortNodeModelProperty
{
    internal const string SystemCategory = "General"; // Categories are not localized

    public string Category { get; } = SystemCategory;
    public Dictionary<string, object[]>? DependentProperties => [];
    public required string Name { get; set; }
    public abstract object? Value { get; set; }
}

internal sealed class DataPortTreeNodeSystemProperty<TData> : DataPortNodeModelSystemProperty, IDataPortNodeModelProperty<TData>
{
    public List<TData> AvailableValues { get; init; } = [];
    public TData? TypedValue { get; set; }
    public override object? Value
    {
        get => TypedValue;
        set => TypedValue = value is null ? default : (TData)value;
    }
}
