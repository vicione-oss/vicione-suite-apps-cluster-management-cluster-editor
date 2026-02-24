using System;
using System.Collections.Generic;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;

internal static class DataPortNodeModelExtensions
{
    public static DataPortChildNodeModel? FindNode(this IEnumerable<DataPortNodeModel> nodes, Guid nodeId)
    {
        foreach (var node in nodes)
        {
            if (node is DataPortChildNodeModel child && child.Id.Value == nodeId)
                return child;

            var subNode = node.Children.FindNode(nodeId);
            if (subNode is not null)
                return subNode;
        }
        return null;
    }

    public static DataPortRootNodeModel GetRootNode(this DataPortNodeModel node)
        => node switch
        {
            DataPortChildNodeModel childNode => childNode.RootNode,
            DataPortRootNodeModel treeRoot => treeRoot,
            _ => throw new ArgumentException($"Unknown derivate of {nameof(DataPortNodeModel)}."),
        };
}
