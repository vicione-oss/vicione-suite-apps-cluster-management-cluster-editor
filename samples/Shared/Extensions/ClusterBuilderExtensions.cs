using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;

namespace Shared.Extensions;

public static class ClusterBuilderExtensions
{
    public static IClusterBuilder AddDemoElements(this IClusterBuilder builder)
    {
        if (builder.Cache.EngineIds.Count == 0)
        {
            var nodeGroup = builder.Editors.Cluster.AddNodeGroup("NodeGroup");
            var node = builder.Editors.NodeGroup.AddNode(nodeGroup, "Node");
            var application = builder.Editors.Node.AddApplication(node, ClusterApplicationType.CoreOsStandalone, "Application");
            var engineHost = builder.Editors.Application.AddEngineHost(application, "EngineHost");

            builder.Editors.EngineHost.AddEngine(engineHost, "Demo Engine 1");
            builder.Editors.EngineHost.AddEngine(engineHost, "Demo Engine 2");
        }

        return builder;
    }
}
