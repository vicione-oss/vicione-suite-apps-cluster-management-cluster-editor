using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Factories;

internal static class PublishedConnectorTooltipEntriesFactory
{
    public static IEnumerable<PublishedConnectorTooltipEntry> Create(IConnector connector, Guid currentContainerId)
    {
        var isInput = connector is ConnectorInput or ContainerConnectorInput;
        return connector.Links
            .Where(l => !l.Visible && l.SourceConnector is not null && l.DestinationConnector is not null)
            .Select(l => new PublishedConnectorTooltipEntry()
            {
                IsSameLevel = GetIsSameLevel(isInput ? l.SourceConnector : l.DestinationConnector, currentContainerId),
                Path = GetPath(isInput ? l.SourceConnector : l.DestinationConnector)
            })
            .OrderByDescending(l => l.IsSameLevel)
            .ThenBy(l => l.Path);
    }

    private static bool GetIsSameLevel(Connector? connector, Guid currentContainerId)
        => connector?.FunctionBlock.Container.Id == currentContainerId;

    private static string GetPath(Connector? connector)
    {
        if (connector is null)
            return "null";

        var path = "";
        var container = connector.FunctionBlock.Container;

        while (container is not null)
        {
            path = $"{container.Name}.{path}";
            container = (container as ChildContainer)?.Parent;
        }

        return $"{path}{connector.FunctionBlock.Name}.{connector.Name}";
    }
}
