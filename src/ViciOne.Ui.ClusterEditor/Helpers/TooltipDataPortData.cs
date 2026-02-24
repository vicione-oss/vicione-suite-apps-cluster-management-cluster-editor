using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.Comparer;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Helpers;

internal static class TooltipDataPortData
{
    private static readonly CompositeFormat s_compositeMoreLinks = CompositeFormat.Parse(Localization.TooltipData.MoreLinks);

    private static List<List<string>?> GetDataPortMarkerTooltipContent(Datastore datastore, BlockNodeConnector connector)
    {
        var content = new List<List<string>?>();

        var marker = connector.DataPortConnectorMarker;
        var maxTooltipLines = 20;

        var paths = connector.IsInput
            ? marker.Links.Select(l => l.SourceDataPortTreeNode!.GetPath(datastore) + "." + l.SourceDataPortTreeNode!.Name)
            : marker.Links.Select(l => l.DestinationDataPortTreeNode!.GetPath(datastore) + "." + l.DestinationDataPortTreeNode!.Name);

        var dataPortPaths = new List<string>();
        dataPortPaths.AddRange(paths.OrderBy(p => p, AlphaNumericComparer<string>.Default));

        var overflow = 0;
        var pathCount = dataPortPaths.Count;
        if (pathCount > maxTooltipLines)
        {
            overflow = pathCount - maxTooltipLines + 1;
            pathCount = maxTooltipLines - 1;
        }

        for (var i = 0; i < pathCount; i++)
            content.Add([dataPortPaths[i]]);

        if (overflow > 0)
            content.Add([string.Format(CultureInfo.InvariantCulture, s_compositeMoreLinks, overflow)]);

        return content;
    }

    public static TooltipInfo GetDataPortMarkerTooltipInfo(Datastore datastore, MouseEventArgs e, BlockNodeConnector connector, Rectangle parentBounds)
    {
        var content = GetDataPortMarkerTooltipContent(datastore, connector);

        var info = new TooltipInfo()
        {
            Header = connector.GetTooltipHeader(),
            ParentBounds = parentBounds,
            PosX = Convert.ToInt32(e.ClientX),
            PosY = Convert.ToInt32(e.ClientY),
            Type = TooltipInfoType.Line
        };
        info.Content.AddRange(content);

        return info;
    }

    private static List<List<string>?> GetDataPortTooltipContent(DataPortTreeNode dataPortTreeNode)
    {
        var content = new List<List<string>?>();

        var maxTooltipLines = 20;
        var sourceConnectorsCount = 0;
        var sourceConnectorsMax = 0;
        var sourceConnectorsOverflow = 0;
        var destinationConnectorsCount = 0;
        var destinationConnectorsMax = 0;
        var destinationConnectorsOverflow = 0;

        var sourceConnectors = dataPortTreeNode.Links
            .Select(l => l.SourceConnector)
            .OfType<Connector>()
            .ToArray();
        var destinationConnectors = dataPortTreeNode.Links
            .Select(l => l.DestinationConnector)
            .OfType<Connector>()
            .ToArray();

        var hasSourceConnectors = sourceConnectors.Length != 0;
        var hasDestinationConnectors = destinationConnectors.Length != 0;
        var showDivider = hasSourceConnectors && hasDestinationConnectors;
        var showSourceConnectorsMoreMessage = false;
        var showDestinationConnectorsMoreMessage = false;

        if (showDivider)
        {
            sourceConnectorsMax = sourceConnectorsCount = sourceConnectors.Length;
            destinationConnectorsMax = destinationConnectorsCount = destinationConnectors.Length;
            var lineCount = 0;
            if (sourceConnectorsMax + destinationConnectorsMax > maxTooltipLines)
            {
                var sourceDiff = sourceConnectorsMax - (maxTooltipLines / 2);
                if (sourceDiff <= 0)
                {
                    lineCount = sourceConnectorsMax;
                }
                else
                {
                    sourceConnectorsCount = maxTooltipLines - Math.Min(destinationConnectorsMax, maxTooltipLines / 2) - 1;
                    sourceConnectorsOverflow = sourceConnectorsMax - sourceConnectorsCount;
                    showSourceConnectorsMoreMessage = sourceConnectorsOverflow > 0;
                    lineCount = sourceConnectorsCount + 1;
                }

                var leftLineCount = maxTooltipLines - lineCount;
                if (destinationConnectorsMax > leftLineCount)
                {
                    destinationConnectorsCount = leftLineCount - 1;
                    destinationConnectorsOverflow = destinationConnectorsMax - leftLineCount + 1;
                    showDestinationConnectorsMoreMessage = true;
                }
            }
        }
        else
        {
            if (hasSourceConnectors)
            {
                sourceConnectorsMax = sourceConnectorsCount = sourceConnectors.Length;
                if (sourceConnectorsMax > maxTooltipLines)
                {
                    sourceConnectorsCount = maxTooltipLines - 1;
                    showDestinationConnectorsMoreMessage = true;
                    sourceConnectorsOverflow = sourceConnectorsMax - sourceConnectorsCount;
                }
            }

            if (hasDestinationConnectors)
            {
                destinationConnectorsMax = destinationConnectorsCount = destinationConnectors.Length;
                if (destinationConnectorsMax > maxTooltipLines)
                {
                    destinationConnectorsCount = maxTooltipLines - 1;
                    showSourceConnectorsMoreMessage = true;
                    destinationConnectorsOverflow = destinationConnectorsMax - destinationConnectorsCount;
                }
            }
        }

        if (hasSourceConnectors)
        {
            content.Add([Localization.TooltipData.ToSend + ":"]);
            content.Add([string.Empty]);
        }

        for (var i = 0; i < sourceConnectorsCount; i++)
            content.Add([sourceConnectors[i].GetPath()]);

        if (showSourceConnectorsMoreMessage)
            content.Add([string.Format(CultureInfo.InvariantCulture, s_compositeMoreLinks, sourceConnectorsOverflow)]);

        if (showDivider)
        {
            content.Add([string.Empty]);
            content.Add(null);
        }

        if (hasDestinationConnectors)
        {
            content.Add([Localization.TooltipData.ToReceive + ":"]);
            content.Add([string.Empty]);
        }

        for (var i = 0; i < destinationConnectorsCount; i++)
            content.Add([destinationConnectors[i].GetPath()]);

        if (showDestinationConnectorsMoreMessage)
            content.Add([string.Format(CultureInfo.InvariantCulture, s_compositeMoreLinks, destinationConnectorsOverflow)]);

        return content;
    }

    public static TooltipInfo GetDataPortTooltipInfo(Datastore datastore, MouseEventArgs e, DataPortNodeModel treeNode, Rectangle parentBounds)
    {
        var info = new TooltipInfo()
        {
            ParentBounds = parentBounds,
            PosX = Convert.ToInt32(e.ClientX),
            PosY = Convert.ToInt32(e.ClientY),
            Type = TooltipInfoType.Line
        };

        if (datastore.Builder.Cache.DataPortTreeNodeGuids.TryGetValue(treeNode.Id.Value, out var dataPortTreeNode))
        {
            var content = GetDataPortTooltipContent(dataPortTreeNode);
            info.Content.AddRange(content);
        }

        if (info.Content.Count == 0)
            info.Content.Add([treeNode.DisplayText]);
        else
            info.Header = treeNode.DisplayText;

        return info;
    }
}
