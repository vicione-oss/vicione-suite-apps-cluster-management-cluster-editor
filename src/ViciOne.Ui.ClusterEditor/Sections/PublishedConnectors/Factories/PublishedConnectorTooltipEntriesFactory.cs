using System;
using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Factories;

internal static class PublishedConnectorTooltipEntriesFactory
{
    public static List<PublishedConnectorTooltipEntry> Create(IConnector connector, Guid currentContainerId)
    {
        var isInput = connector is ConnectorInput or ContainerConnectorInput;
        var entries = new List<PublishedConnectorTooltipEntry>();

        foreach (var l in connector.Links)
        {
            if (l.Visible || l.SourceConnector is null || l.DestinationConnector is null)
                continue;

            Connector? target = isInput ? l.SourceConnector : l.DestinationConnector;
            entries.Add(new PublishedConnectorTooltipEntry
            {
                IsSameLevel = GetIsSameLevel(target, currentContainerId),
                Path = GetPath(target)
            });
        }

        entries.Sort((a, b) =>
        {
            var cmp = b.IsSameLevel.CompareTo(a.IsSameLevel);
            return cmp != 0 ? cmp : string.Compare(a.Path, b.Path, StringComparison.Ordinal);
        });

        return entries;
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
