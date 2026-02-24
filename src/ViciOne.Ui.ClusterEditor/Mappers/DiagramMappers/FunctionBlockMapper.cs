using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Blazor.Diagrams.Core.Geometry;
using Microsoft.JSInterop;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Helpers;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.Shared.Dx.Services;

namespace ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;

internal static partial class FunctionBlockMapper
{
    internal static async Task<FunctionBlockNode> CreateNodeAsync(
        ComparerService comparerService,
        Datastore datastore,
        DiagramService diagramService,
        FunctionBlock functionBlock,
        IJSRuntime jsRuntime,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var position = new Point(functionBlock.X ?? 0, functionBlock.Y ?? 0);
        var node = new FunctionBlockNode(position)
        {
            ImageSrc = null, // TODO: functionBlock hat im Moment noch keine Image Eigenschaften
            ImageText = GetNodeImageText(datastore.Builder.ResolveFunctionBlockDesign(functionBlock.DesignId).Name),
            Name = functionBlock.Name,
            NameBackgroundColor = functionBlock.BackColor ?? BlockNodeColors.BackgroundDefault,
            NameForeColor = functionBlock.ForeColor ?? BlockNodeColors.ForegroundDefault
        };

        node.SetEngineDisplayText(EngineDisplayText.Get(datastore.Builder, functionBlock));
        node.SetCycleFrequency(functionBlock.CycleFrequency);
        node.SetRunMode(functionBlock.RunMode);

        node.Connectors.AddRange(GenerateConnectors(comparerService, datastore, diagramService, functionBlock, node));
        node.CalculateDisplayAllConnectors();
        cancellationToken.ThrowIfCancellationRequested();

        node.NameFieldHeight = await jsRuntime.MeasureNameFieldHeightAsync(node.Name, cancellationToken);
        node.UpdateSize();
        node.PrecomputePortsInformation();

        return node;
    }

    private static List<BlockNodeConnector?[]> GenerateConnectors(
        ComparerService comparerService,
        Datastore datastore,
        DiagramService diagramService,
        FunctionBlock functionBlock,
        FunctionBlockNode functionBlockNode)
    {
        var result = new List<BlockNodeConnector?[]>();
        var rowCount = 0;
        var currentConnectorCount = 0;

        // TODO: Connector.Index Eigenschaft nutzen wenn diese gefüllt ist
        var count = 0;
        var inputConnectors = functionBlock.SystemInputs.ToDictionary(c => count++);

        var inputIdx = 0;
        foreach (var dataInput in functionBlock.ProcessDataInputs)
            inputConnectors.Add(inputIdx++ + BlockNodeLayout.SystemConnectorRows, dataInput);

        count = 0;
        var outputConnectors = functionBlock.SystemOutputs.ToDictionary(c => count++);

        var outputIdx = 0;
        foreach (var dataOutput in functionBlock.ProcessDataOutputs)
            outputConnectors.Add(outputIdx++ + BlockNodeLayout.SystemConnectorRows, dataOutput);

        var maxConnectorCount = inputConnectors.Count + outputConnectors.Count;

        while (currentConnectorCount < maxConnectorCount || rowCount < BlockNodeLayout.MinimumConnectorRows)
        {
            var row = new BlockNodeConnector?[2];
            inputConnectors.TryGetValue(rowCount, out var inputConnector);
            outputConnectors.TryGetValue(rowCount, out var outputConnector);

            if (inputConnector is not null)
            {
                row[0] = ConnectorMapper.CreateNodeConnector(comparerService, datastore, diagramService, functionBlockNode, inputConnector);
                currentConnectorCount++;
            }
            if (outputConnector is not null)
            {
                row[1] = ConnectorMapper.CreateNodeConnector(comparerService, datastore, diagramService, functionBlockNode, outputConnector);
                currentConnectorCount++;
            }
            result.Add(row);
            rowCount++;
        }

        return result;
    }

    private static string GetNodeImageText(string functionBlockDesignName)
    {
        if (string.IsNullOrEmpty(functionBlockDesignName))
            return string.Empty;

        var name = ImageTextRegex().Replace(functionBlockDesignName.Replace("_", " ", StringComparison.Ordinal), " ");
        name = name.Replace("Vici One", "ViciOne", StringComparison.OrdinalIgnoreCase);
        name = name.Replace("RS 232", "RS232", StringComparison.OrdinalIgnoreCase);
        name = name.Replace("RS 485", "RS485", StringComparison.OrdinalIgnoreCase);
        name = name.Replace("Mini Server", "MiniServer", StringComparison.OrdinalIgnoreCase);
        name = name.Replace("Sensor Bar", "SensorBar", StringComparison.OrdinalIgnoreCase);
        name = name.Replace("BA Cnet", "BACnet", StringComparison.OrdinalIgnoreCase);

        return name;
    }

    [GeneratedRegex("(?<=[A-Z])(?=[A-Z][a-z])|(?<=[^A-Z])(?=[A-Z])|(?<=[A-Za-z])(?=[^A-Za-z])")]
    private static partial Regex ImageTextRegex();

    internal static void PropertyChanged(
        Datastore datastore,
        FunctionBlock functionBlock,
        FunctionBlockNode functionBlockNode,
        string propertyName)
    {
        switch (propertyName)
        {
            case nameof(functionBlock.BackColor):
                functionBlockNode.NameBackgroundColor = functionBlock.BackColor ?? BlockNodeColors.BackgroundDefault;
                break;
            case nameof(functionBlock.CycleFrequency):
                functionBlockNode.SetCycleFrequency(functionBlock.CycleFrequency);
                break;
            case nameof(functionBlock.Engine):
                functionBlockNode.SetEngineDisplayText(EngineDisplayText.Get(datastore.Builder, functionBlock));
                break;
            case nameof(functionBlock.ForeColor):
                functionBlockNode.NameForeColor = functionBlock.ForeColor ?? BlockNodeColors.ForegroundDefault;
                break;
            case nameof(functionBlock.X):
            case nameof(functionBlock.Y):
                functionBlockNode.SetPosition(functionBlock.X ?? 0, functionBlock.Y ?? 0);
                break;
            case nameof(functionBlock.Name):
                functionBlockNode.Name = functionBlock.Name;
                break;
            case nameof(functionBlock.RunMode):
                functionBlockNode.SetRunMode(functionBlock.RunMode);
                break;
        }

        functionBlockNode.RefreshAll();
    }

    internal static void UpdatePosition(Datastore datastore, FunctionBlockNode functionBlockNode)
    {
        var functionBlock = datastore.DataflowDiagramMapping.GetModel(functionBlockNode);
        datastore.Builder.Editors.FunctionBlock.SetLocation(
            functionBlock,
            new((int)functionBlockNode.Position.X, (int)functionBlockNode.Position.Y)
        );
    }
}
