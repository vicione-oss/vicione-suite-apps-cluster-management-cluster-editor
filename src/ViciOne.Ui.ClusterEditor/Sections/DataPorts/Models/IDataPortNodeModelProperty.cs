using System.Collections.Generic;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

public interface IDataPortNodeModelProperty
{
    string Category { get; }
    object? DefaultValue { get; set; }
    string DependencyId { get; }
    Dictionary<string, object[]>? DependentProperties { get; }
    string Name { get; set; }
    object? Value { get; set; }
}

public interface IDataPortNodeModelProperty<T> : IDataPortNodeModelProperty
{
    T? TypedDefaultValue { get; set; }
    T? TypedValue { get; set; }
}
