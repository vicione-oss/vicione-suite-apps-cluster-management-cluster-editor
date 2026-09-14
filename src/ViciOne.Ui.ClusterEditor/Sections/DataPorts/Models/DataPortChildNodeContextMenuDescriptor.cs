
using System.Collections.Generic;
using ViciOne.Tree.Builder.NodeTypes;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

public sealed class DataPortChildNodeContextMenuDescriptor
{
    public List<DataPortChildNodeContextMenuDescriptor> Children { get; } = [];
    public required string IconName { get; init; }
    public bool IsDataPoint { get; init; }
    public required string Name { get; init; }
    public required NodeReference NodeReference { get; init; }
    public required DataPortNodeModel ParentNode { get; init; }
}
