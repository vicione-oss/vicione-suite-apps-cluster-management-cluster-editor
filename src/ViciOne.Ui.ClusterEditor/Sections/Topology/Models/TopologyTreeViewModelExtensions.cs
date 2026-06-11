using System.Collections.Generic;

namespace ViciOne.Ui.ClusterEditor.Sections.Topology.Models;

internal static class TopologyTreeViewModelExtensions
{
    public static IEnumerable<TopologyTreeViewModel> GetNodeAndDescendants(this TopologyTreeViewModel node)
    {
        yield return node;

        foreach (var child in node.Children)
        {
            foreach (var descendant in child.GetNodeAndDescendants())
                yield return descendant;
        }
    }
}
