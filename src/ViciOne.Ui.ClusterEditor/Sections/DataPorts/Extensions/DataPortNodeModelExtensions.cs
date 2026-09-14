using System;
using System.Collections.Generic;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.TreeEditor.Builder;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;

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

    /// <summary>
    /// Redraws the icon of <paramref name="node"/> and of every node below it.
    /// </summary>
    /// <remarks>
    /// A node's icon depends on its ancestors: an envelope child is greyed out exactly when the
    /// datapoint carrying it is, and a datapoint's arrows follow the DataPort's direction. A change
    /// on one node therefore has to redraw the whole subtree.
    /// </remarks>
    public static void NotifyIconsChangedRecursively(this DataPortNodeModel node, ITreeBuilder builder)
    {
        foreach (var child in node.Children)
            child.NotifyIconsChangedRecursively(builder);

        builder.Notifications.NotifyNodeChanged(node, ChangedNodeDetail.Icons);
    }
}
