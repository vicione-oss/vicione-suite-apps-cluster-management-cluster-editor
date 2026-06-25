using System;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.TreeEditor.Builder;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace ViciOne.Ui.ClusterEditor.Helpers;

internal static class TreeAdapterHelper
{
    /// <summary>
    /// This method sets a filter for a ViciOne.Ui.TreeEditor such that any matches will always display their whole tree,
    /// i.e. all the child nodes of a filter match are shown regardless of themselves matching the filter as well as all parent nodes.
    /// The value the filterText is compared to is ITreeNode.DisplayText.
    /// This also relies on the tree having loaded all of the nodes, it might be necessary to call ITreeBuilder.Helper.Preload() first.
    /// </summary>
    public static void FilterNodesByDisplayText(ITreeBuilder builder, Func<ITreeNode, ITreeNode?> resolveParent, string filterText)
    {
        if (string.IsNullOrWhiteSpace(filterText))
            builder.Filter.SetFilter([]);
        else
            builder.Filter.SetFilter([FilterFunc]);

        bool FilterFunc(ITreeNode node)
        {
            if (node is not IClusterEditorTreeNode ceNode)
                return false;

            if (ceNode.Name.Contains(filterText, StringComparison.InvariantCultureIgnoreCase))
                return true;

            var currentNode = resolveParent(node);
            while (currentNode is not null)
            {
                if (currentNode is IClusterEditorTreeNode parentCeNode &&
                    parentCeNode.Name.Contains(filterText, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }

                currentNode = resolveParent(currentNode);
            }

            return false;
        }
    }
}
