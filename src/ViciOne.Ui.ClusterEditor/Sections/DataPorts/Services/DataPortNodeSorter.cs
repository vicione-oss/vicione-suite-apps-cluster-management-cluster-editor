using System.Collections.Generic;
using System.Linq;
using ViciOne.Tree.Builder.Extensions;
using ViciOne.Ui.ClusterEditor.Models.Comparer;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.TreeEditor.Builder;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

internal static class DataPortNodeSorter
{
    // A datapoint that carries envelope children can take further children, but it is still a value
    // node and belongs with the other datapoints rather than with the folders.
    internal static bool IsContainerNode(DataPortNodeModel node)
        => node.PossibleChildren.Any() && node is not DataPortChildNodeModel { IsDataPoint: true };

    internal static void SortChildren(DataPortNodeModel dataPortParentNode, bool sortNonParentChildren)
    {
        var children = dataPortParentNode.Children;
        children = [.. SortNodes(children, sortNonParentChildren)];
        dataPortParentNode.Children.Clear();
        dataPortParentNode.Children.AddRange(children);
    }

    internal static void SortNodeChildren(DataPortNodeModel node, ITreeBuilder builder)
    {
        if (IsContainerNode(node))
        {
            if (node.GetRootNode().TryGetPathToNode(node, out var path) && path.Count > 1)
            {
                var parent = path.ElementAt(1);

                if (parent is DataPortNodeModel parentTreeNode)
                {
                    SortChildren(parentTreeNode, false);
                    builder.Notifications.NotifyChildrenChanged(parentTreeNode);
                }
            }
        }
    }

    internal static IEnumerable<DataPortNodeModel> SortNodes(IEnumerable<DataPortNodeModel> children, bool sortNonParentChildren)
    {
        var childrenList = children.ToList();
        var parentChildNodes = childrenList.Where(IsContainerNode).ToList();
        var nonParentChildNodes = childrenList.Except(parentChildNodes).ToList();
        parentChildNodes = [.. parentChildNodes.OrderBy(c => c.Name, AlphaNumericComparer<string>.Default)];

        if (sortNonParentChildren)
        {
            nonParentChildNodes = [.. nonParentChildNodes.OrderBy(c => c.Name, AlphaNumericComparer<string>.Default)];
        }

        children = [.. parentChildNodes, .. nonParentChildNodes];
        return children;
    }
}
