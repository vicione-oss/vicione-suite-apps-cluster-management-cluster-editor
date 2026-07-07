using System.Collections.Generic;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

internal abstract class DataPortNodeModelSystemProperty : IDataPortNodeModelProperty
{
    internal const string SystemCategory = "General"; // Categories are not localized
    private readonly Dictionary<string, object[]>? _dependentProperties = [];

    public string Category { get; } = SystemCategory;
    public abstract object? DefaultValue { get; set; }
    public string DependencyId => Name;
    public Dictionary<string, object[]>? DependentProperties => _dependentProperties;
    public required string Name { get; set; }
    public abstract object? Value { get; set; }
}

internal sealed class DataPortTreeNodeSystemProperty<TData> : DataPortNodeModelSystemProperty, IDataPortNodeModelProperty<TData>
{
    public List<TData> AvailableValues { get; init; } = [];
    public override object? DefaultValue
    {
        get => TypedDefaultValue;
        set => TypedDefaultValue = value is null ? default : (TData)value;
    }
    public TData? TypedDefaultValue { get; set; }
    public TData? TypedValue { get; set; }
    public override object? Value
    {
        get => TypedValue;
        set => TypedValue = value is null ? default : (TData)value;
    }
}
