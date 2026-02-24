using System.Linq;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Models;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;

namespace ViciOne.Ui.ClusterEditor.Sections.Topology.Factories;

public static class TopologyTreeFactory
{
    public static TopologyTreeViewModel BuildTreeFromClusterNodeGroup(ClusterNodeGroup nodeGroup)
    {
        return BuildTreeFromClusterNodeGroupRecursive(nodeGroup, null);

        static TopologyTreeViewModel BuildTreeFromClusterNodeGroupRecursive(ClusterNodeGroup nodeGroup, TopologyTreeViewModel? parent)
        {
            var resultingNodeGroup = CreateClusterNodeGroupNode(nodeGroup, parent);

            resultingNodeGroup.Children.AddRange(nodeGroup.NodeGroups.Select(gn => CreateClusterNodeGroupNode(gn, resultingNodeGroup)));
            resultingNodeGroup.Children.AddRange(nodeGroup.Nodes.Select(n =>
            {
                var resultingNode = CreateClusterNodeNode(n, resultingNodeGroup);
                resultingNode.Children.AddRange(n.Applications.Select(a =>
                {
                    var resultingApplication = CreateApplicationNode(a, resultingNode);
                    resultingApplication.Children.AddRange(a.EngineHosts.Select(eh =>
                    {
                        var resultingEngineHost = CreateEngineHostNode(eh, resultingApplication);
                        resultingEngineHost.Children.AddRange(eh.Engines.Select(e => CreateEngineNode(e, resultingEngineHost)));

                        return resultingEngineHost;
                    }));

                    return resultingApplication;
                }));

                return resultingNode;
            }));

            return resultingNodeGroup;
        }
    }

    public static TopologyTreeViewModel CreateApplicationNode(ClusterApplication application, TopologyTreeViewModel parent)
        => new(application)
        {
            DisplayText = application.Name,
            Expanded = true,
            Id = new GuidNodeIdentifier(application.Id),
            Parent = parent,
        };

    public static TopologyTreeViewModel CreateClusterNodeGroupNode(ClusterNodeGroup nodeGroup, TopologyTreeViewModel? parent)
        => new(nodeGroup)
        {
            DisplayText = nodeGroup.Name,
            Expanded = true,
            Id = new GuidNodeIdentifier(nodeGroup.Id),
            Parent = parent,
        };

    public static TopologyTreeViewModel CreateClusterNodeNode(ClusterNode node, TopologyTreeViewModel parent)
        => new(node)
        {
            DisplayText = node.Name,
            Expanded = true,
            Id = new GuidNodeIdentifier(node.Id),
            Parent = parent,
        };

    public static TopologyTreeViewModel CreateEngineHostNode(EngineHost engineHost, TopologyTreeViewModel parent)
        => new(engineHost)
        {
            DisplayText = engineHost.Name,
            Expanded = true,
            Id = new GuidNodeIdentifier(engineHost.Id),
            Parent = parent,
        };

    public static TopologyTreeViewModel CreateEngineNode(Cluster.Model.Engine engine, TopologyTreeViewModel parent)
        => new(engine)
        {
            DisplayText = engine.Name,
            Expanded = true,
            Id = new GuidNodeIdentifier(engine.Id),
            Parent = parent,
        };
}
