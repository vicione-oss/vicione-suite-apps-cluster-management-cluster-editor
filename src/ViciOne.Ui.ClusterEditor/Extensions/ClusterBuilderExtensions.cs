using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Ui.ClusterEditor.Constants;

namespace ViciOne.Ui.ClusterEditor.Extensions;

internal static class ClusterBuilderExtensions
{
    public static IEnumerable<DataPort> GetDataPortsLinkedToConnector(this IClusterBuilder builder, IConnector connector)
    {
        if (connector is IConnectorInput input)
        {
            return input.Links
                .Where(k => k.SourceDataPortTreeNode is not null)
                .Distinct()
                .Select(k => builder.Cache.GetDataPort(k.SourceDataPortTreeNode!));
        }
        else if (connector is IConnectorOutput output)
        {
            return output.Links
                .Where(k => k.DestinationDataPortTreeNode is not null)
                .Distinct()
                .Select(k => builder.Cache.GetDataPort(k.DestinationDataPortTreeNode!));
        }
        else
        {
            throw new ArgumentException("Unknown Connector type.", nameof(connector));
        }
    }

    public static IEnumerable<FunctionBlockDesign> GetFunctionBlockDesigns(this IClusterBuilder builder)
    {
        var lst = new List<FunctionBlockDesign>();

        foreach (var guid in builder.Cache.FunctionBlockDesigns.Keys)
            lst.Add(builder.ResolveFunctionBlockDesign(guid));

        return lst;
    }

    public static void InitSettings(this IClusterBuilder builder)
    {
        builder.Settings.AllowTypeConversionViaLink = false;
        builder.Settings.GridEnabled = true;
        builder.Settings.GridSize = DiagramSettings.DefaultGridSize;
        builder.Settings.LabelMinimumGridCells = DiagramSettings.LabelMinimumGridCells;
    }
}
