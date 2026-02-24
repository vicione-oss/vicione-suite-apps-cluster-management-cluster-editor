using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Ui.ClusterEditor.Sections.Topology.Models;

internal static class TopologyTreeViewModelExtensions
{
    public static IEnumerable<TopologyTreeViewModel> GetNodeAndDescendants(this TopologyTreeViewModel node)
        => new[] { node }.Concat(node.Children.SelectMany((child) => child.GetNodeAndDescendants()));
}
