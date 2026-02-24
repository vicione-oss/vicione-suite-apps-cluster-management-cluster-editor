using ViciOne.Ui.TreeEditor.Builder;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace ViciOne.Ui.ClusterEditor.Extensions;

internal static class ITreeNodeExtensions
{
    internal static void ScrollToNode(this ITreeNode treeNode, ITreeBuilder treeBuilder)
    {
        treeBuilder.Expansion.ExpandToNode(treeNode);
        treeBuilder.Scrolling.RequestScrollToNode(treeNode);
        treeBuilder.Selection.ChangeSelection(treeNode, true, true);
    }
}
