using System;
using System.Linq;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

internal static class DataPortChildNodeModelFactory
{
    internal static DataPortChildNodeModel CreateDataPortChildNodeModel(DataPortChildNodeContextMenuDescriptor descriptor, DataPortNodeModel parentNode)
    {
        var rootNode = parentNode.GetRootNode();

        if (!rootNode.Builder.NodeTypes.TryGetValue(descriptor.NodeReference.Id, out var nodeType))
            throw new InvalidOperationException($"NodeReference with Id {descriptor.NodeReference.Id} is not part of the current ruleset");

        return new DataPortChildNodeModel
        {
            AvailableIcons = nodeType.Icons,
            CanHaveChildren = nodeType.ChildNodes.Length != 0,
            Icon = nodeType.Icons.FirstOrDefault(),
            IsDataPoint = nodeType.IsDataPoint(),
            Name = nodeType.Name,
            NameIsReadOnly = nodeType.NameIsReadOnly,
            NodeReference = descriptor.NodeReference,
            Parent = parentNode,
            Properties = [.. rootNode.Builder.GetProperties(nodeType)],
            RootNode = rootNode,
            TransferDirections = nodeType.GetInheritedTransferDirections(parentNode),
        };
    }
}
