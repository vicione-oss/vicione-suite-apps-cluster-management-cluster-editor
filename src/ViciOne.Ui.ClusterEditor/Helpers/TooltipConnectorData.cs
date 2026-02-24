using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Core.Contracts;
using ViciOne.Ui.ClusterEditor.Localization.Resources;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;

namespace ViciOne.Ui.ClusterEditor.Helpers;

internal static class TooltipConnectorData
{
    private static List<List<string>?> GetConnectorTooltipContent(ClusterBuilder builder, IConnector connector)
    {
        var content = new List<List<string>?>();

        var design = builder.ResolveConnectorDesign(connector.GetUnderlyingConnector());

        if (!string.IsNullOrWhiteSpace(connector.Description))
        {
            content.Add([nameof(connector.Description), connector.Description]);
            content.Add(null);
        }

        content.Add([nameof(connector.Id), connector.Id.ToString()]);
        content.Add([Ui.Localization.Resources.TechnicalTerms.DataType, DataTypeCompatibilityValidator.DetermineValueType(design.ConnectorType).Name]);
        content.Add([TechnicalTerms.DefaultValue, design.DefaultValue?.ToString() ?? "null"]);
        content.Add([nameof(connector.Value), GetValue(connector)?.ToString() ?? "null"]);
        content.Add(null);

        if (connector is IConnectorInput inputConnector)
        {
            content.Add([nameof(inputConnector.CrossSourcePoolingStrategy), inputConnector.CrossSourcePoolingStrategy.ToString()]);

            if (inputConnector.CrossSourceAggregatingPooling is not null)
            {
                content.Add([nameof(inputConnector.CrossSourceAggregatingPooling), inputConnector.CrossSourceAggregatingPooling.Name]);
            }

            content.Add(null);
        }

        content.Add([nameof(connector.EventEnabled), connector.EventEnabled.ToString()]);
        content.Add([nameof(connector.MarkAsChangedOnlyIfNotEqual), connector.MarkAsChangedOnlyIfNotEqual.ToString()]);
        content.Add([nameof(connector.Published), connector.Published.ToString()]);
        content.Add([Localization.TooltipData.LinkCount, connector.Links.Count.ToString(CultureInfo.InvariantCulture)]);

        static object? GetValue(IConnector connectorModel)
            => connectorModel switch
            {
                Connector connector => connector.Value,
                ContainerConnector containerConnector => containerConnector.Value,
                _ => null,
            };

        return content;
    }

    public static TooltipInfo GetConnectorTooltipInfo(ClusterBuilder clusterBuilder, MouseEventArgs e, BlockNodeConnector connector, Rectangle parentBounds)
    {
        var content = GetConnectorTooltipContent(clusterBuilder, connector.Connector);

        var info = new TooltipInfo()
        {
            Header = connector.GetTooltipHeader(),
            ParentBounds = parentBounds,
            PosX = Convert.ToInt32(e.ClientX),
            PosY = Convert.ToInt32(e.ClientY),
            Type = TooltipInfoType.Table
        };
        info.Content.AddRange(content);

        return info;
    }

    public static TooltipInfo GetParentConnectorTooltipInfo(ClusterBuilder clusterBuilder, MouseEventArgs e, BlockNodeConnector connector, Rectangle parentBounds)
    {
        ArgumentNullException.ThrowIfNull(connector.ParentContainerConnector, nameof(connector.ParentContainerConnector));

        var content = GetConnectorTooltipContent(clusterBuilder, connector.ParentContainerConnector);

        var info = new TooltipInfo()
        {
            Header = connector.GetTooltipHeader(true),
            ParentBounds = parentBounds,
            PosX = Convert.ToInt32(e.ClientX),
            PosY = Convert.ToInt32(e.ClientY),
            Type = TooltipInfoType.Table
        };
        info.Content.AddRange(content);

        return info;
    }
}
