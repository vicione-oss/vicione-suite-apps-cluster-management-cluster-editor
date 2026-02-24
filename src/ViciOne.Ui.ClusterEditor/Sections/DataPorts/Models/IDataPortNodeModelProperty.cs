using System.Collections.Generic;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

public interface IDataPortNodeModelProperty
{
    string Category { get; }
    Dictionary<string, object[]>? DependentProperties { get; }
    string Name { get; set; }
    object? Value { get; set; }
}

public interface IDataPortNodeModelProperty<T> : IDataPortNodeModelProperty
{
    T? TypedValue { get; set; }
}
