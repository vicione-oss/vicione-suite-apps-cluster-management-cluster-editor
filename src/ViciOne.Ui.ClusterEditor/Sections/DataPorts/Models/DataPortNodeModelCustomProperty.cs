using System;
using System.Collections.Generic;
using ViciOne.TreeBuilder.PropertyTypes;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

internal sealed class DataPortNodeModelCustomProperty : IDataPortNodeModelProperty
{
    public required string Category { get; set; }
    public object? DefaultValue { get; set; }
    public string DependencyId => Type.Id;
    public Dictionary<string, object[]>? DependentProperties { get; set; }
    public required Guid Id { get; set; }
    public object? MaxValue { get; set; }
    public object? MinValue { get; set; }
    public required string Name { get; set; }
    public Dictionary<object, string>? PossibleValues { get; set; }
    public required PropertyReference Reference { get; set; }
    public required Type RuntimeType { get; set; }
    public required PropertyType Type { get; set; }
    public object? Value { get; set; }
}
