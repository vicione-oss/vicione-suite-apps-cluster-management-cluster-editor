using System;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Extensions;

namespace ViciOne.Ui.ClusterEditor.Sections.Topology.Factories;

internal static class DataItemCloneFactory
{
    public static object CreateClone(object dataItem)
        => dataItem switch
        {
            ClusterNodeGroup nodeGroup
                => nodeGroup.ShallowCopy(),

            ClusterNode node
                => node.ShallowCopy(),

            ClusterApplication application
                => application.ShallowCopy(),

            EngineHost engineHost
                => engineHost.ShallowCopy(),

            Cluster.Model.Engine engine
                => engine.ShallowCopy(),

            _ => throw new InvalidOperationException($"Can't create clone for {dataItem.GetType().Name}.")
        };
}
