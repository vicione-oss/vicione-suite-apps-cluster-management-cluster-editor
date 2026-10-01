using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Blazor.Diagrams.Core.Geometry;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Helpers;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;

internal static partial class FunctionBlockMapper
{
    internal static FunctionBlockNode CreateNode(
        ComparerService comparerService,
        IDatastoreState datastoreState,
        DiagramService diagramService,
        FunctionBlock functionBlock,
        int nameFieldHeight)
    {
        var position = new Point(functionBlock.X ?? 0, functionBlock.Y ?? 0);
        var node = new FunctionBlockNode(position)
        {
            ImageSrc = null,
            ImageText = GetNodeImageText(datastoreState.Builder.ResolveFunctionBlockDesign(functionBlock.DesignId).Name),
            Name = functionBlock.Name,
            NameBackgroundColor = functionBlock.BackColor ?? BlockNodeColors.BackgroundDefault,
            NameForeColor = functionBlock.ForeColor ?? BlockNodeColors.ForegroundDefault
        };

        node.SetEngineDisplayText(EngineDisplayText.Get(datastoreState.Builder, functionBlock));
        node.SetCycleFrequency(functionBlock.CycleFrequency);
        node.SetRunMode(functionBlock.RunMode);

        node.Connectors.AddRange(GenerateConnectors(comparerService, datastoreState, diagramService, functionBlock, node));
        node.InvalidateConnectorsCache();
        node.CalculateDisplayAllConnectors();

        node.NameFieldHeight = nameFieldHeight;
        node.UpdateSize();

        return node;
    }

    private static List<BlockNodeConnector?[]> GenerateConnectors(
        ComparerService comparerService,
        IDatastoreState datastoreState,
        DiagramService diagramService,
        FunctionBlock functionBlock,
        FunctionBlockNode functionBlockNode)
    {
        var result = new List<BlockNodeConnector?[]>();
        var rowCount = 0;
        var currentConnectorCount = 0;
        var systemConnectors = new HashSet<IConnector>(functionBlock.GetSystemConnectors());

        var inputConnectors = new Dictionary<int, IConnector>();
        var inputIdx = 0;
        foreach (var sysInput in functionBlock.SystemInputs)
            inputConnectors.Add(inputIdx++, sysInput);

        var index = 0;
        foreach (var dataInput in functionBlock.ProcessDataInputs)
            inputConnectors.Add(index++ + BlockNodeLayout.SystemConnectorRows, dataInput);

        var outputConnectors = new Dictionary<int, IConnector>();
        var outputIdx = 0;
        foreach (var sysOutput in functionBlock.SystemOutputs)
            outputConnectors.Add(outputIdx++, sysOutput);

        index = 0;
        foreach (var dataOutput in functionBlock.ProcessDataOutputs)
            outputConnectors.Add(index++ + BlockNodeLayout.SystemConnectorRows, dataOutput);

        var maxConnectorCount = inputConnectors.Count + outputConnectors.Count;

        while (currentConnectorCount < maxConnectorCount || rowCount < BlockNodeLayout.MinimumConnectorRows)
        {
            var row = new BlockNodeConnector?[2];
            inputConnectors.TryGetValue(rowCount, out var inputConnector);
            outputConnectors.TryGetValue(rowCount, out var outputConnector);

            if (inputConnector is not null)
            {
                row[0] = ConnectorMapper.CreateNodeConnector(comparerService, datastoreState, diagramService, functionBlockNode, inputConnector, systemConnectors.Contains(inputConnector));
                currentConnectorCount++;
            }
            if (outputConnector is not null)
            {
                row[1] = ConnectorMapper.CreateNodeConnector(comparerService, datastoreState, diagramService, functionBlockNode, outputConnector, systemConnectors.Contains(outputConnector));
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
        IDatastoreState datastoreState,
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
                functionBlockNode.SetEngineDisplayText(EngineDisplayText.Get(datastoreState.Builder, functionBlock));
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

    internal static void UpdatePosition(IDatastoreState datastoreState, FunctionBlockNode functionBlockNode)
    {
        var functionBlock = datastoreState.DataflowDiagramMapping.GetModel(functionBlockNode);
        datastoreState.Builder.Editors.FunctionBlock.SetLocation(
            functionBlock,
            new((int)functionBlockNode.Position.X, (int)functionBlockNode.Position.Y)
        );
    }
}
