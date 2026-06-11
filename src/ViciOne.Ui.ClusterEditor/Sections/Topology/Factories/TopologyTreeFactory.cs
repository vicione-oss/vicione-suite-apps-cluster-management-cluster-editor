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

            foreach (var gn in nodeGroup.NodeGroups)
                resultingNodeGroup.Children.Add(CreateClusterNodeGroupNode(gn, resultingNodeGroup));

            foreach (var n in nodeGroup.Nodes)
            {
                var resultingNode = CreateClusterNodeNode(n, resultingNodeGroup);

                foreach (var a in n.Applications)
                {
                    var resultingApplication = CreateApplicationNode(a, resultingNode);

                    foreach (var eh in a.EngineHosts)
                    {
                        var resultingEngineHost = CreateEngineHostNode(eh, resultingApplication);

                        foreach (var e in eh.Engines)
                            resultingEngineHost.Children.Add(CreateEngineNode(e, resultingEngineHost));

                        resultingApplication.Children.Add(resultingEngineHost);
                    }

                    resultingNode.Children.Add(resultingApplication);
                }

                resultingNodeGroup.Children.Add(resultingNode);
            }

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
