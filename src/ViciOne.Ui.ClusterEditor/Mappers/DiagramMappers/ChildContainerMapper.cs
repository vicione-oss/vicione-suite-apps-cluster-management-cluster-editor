using System;
using System.Collections.Generic;
using System.Linq;
using Blazor.Diagrams.Core.Geometry;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Helpers;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.Shared.Dx.Services;

namespace ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;

internal static class ChildContainerMapper
{
    internal static ChildContainerNode CreateNode(
        ComparerService comparerService,
        IDatastoreState datastoreState,
        DiagramService diagramService,
        ChildContainer childContainer,
        int nameFieldHeight)
    {
        var position = new Point(childContainer.X ?? 0, childContainer.Y ?? 0);
        var node = new ChildContainerNode(position)
        {
            Name = childContainer.Name,
            NameBackgroundColor = childContainer.BackColor ?? BlockNodeColors.BackgroundDefault,
            NameForeColor = childContainer.ForeColor ?? BlockNodeColors.ForegroundDefault
        };

        node.SetEngineDisplayText(EngineDisplayText.Get(datastoreState.Builder, childContainer));

        var nextLevelElements = GetContainerElementCount(childContainer, true);
        var allElements = GetContainerElementCount(childContainer, false);
        node.SetChildrenInformation(nextLevelElements, allElements);

        node.Connectors.AddRange(GenerateConnectors(node, childContainer, comparerService, datastoreState, diagramService));
        node.InvalidateConnectorsCache();
        node.CalculateDisplayAllConnectors();

        node.NameFieldHeight = nameFieldHeight;
        node.UpdateSize();

        return node;
    }

    internal static IEnumerable<BlockNodeConnector?[]> GenerateConnectors(
        ChildContainerNode childContainerNode,
        ChildContainer childContainer,
        ComparerService comparerService,
        IDatastoreState datastoreState,
        DiagramService diagramService)
    {
        var result = new List<BlockNodeConnector?[]>();

        for (var i = 0; i < BlockNodeLayout.SystemConnectorRows; i++)
            result.Add(new BlockNodeConnector?[2]);

        var connectors = childContainer.GetConnectors().ToArray();
        var systemConnectors = connectors.Length > 0
            ? new HashSet<IConnector>(connectors[0].FunctionBlock.GetSystemConnectors())
            : [];
        var inputsByIndex = new Dictionary<uint, IConnector>();
        var outputsByIndex = new Dictionary<uint, IConnector>();
        uint maxIndex = 0;
        foreach (var c in connectors)
        {
            if (c.Index > maxIndex)
                maxIndex = c.Index;

            if (c is ContainerConnectorInput)
                inputsByIndex[c.Index] = c;
            else if (c is ContainerConnectorOutput)
                outputsByIndex[c.Index] = c;
        }

        var maxRowCount = Math.Max(BlockNodeLayout.MinimumConnectorRows - BlockNodeLayout.SystemConnectorRows,
            connectors.Length != 0 ? maxIndex + 1 : 0);

        for (uint i = 0; i < maxRowCount; i++)
        {
            var row = new BlockNodeConnector?[2];
            inputsByIndex.TryGetValue(i, out var inputConnector);
            outputsByIndex.TryGetValue(i, out var outputConnector);

            if (inputConnector is not null)
                row[0] = ConnectorMapper.CreateNodeConnector(comparerService, datastoreState, diagramService, childContainerNode, inputConnector, systemConnectors.Contains(inputConnector));
            if (outputConnector is not null)
                row[1] = ConnectorMapper.CreateNodeConnector(comparerService, datastoreState, diagramService, childContainerNode, outputConnector, systemConnectors.Contains(outputConnector));

            result.Add(row);
        }

        return result;
    }

    private static int GetContainerElementCount(ChildContainer container, bool nextLevelOnly)
    {
        var result = container.FunctionBlocks.Count + container.Containers.Count;

        if (nextLevelOnly)
            return result;

        foreach (var cont in container.Containers)
            result += GetContainerElementCount(cont, false);

        return result;
    }

    internal static void PropertyChanged(
        ChildContainer container,
        ChildContainerNode containerNode,
        IDatastoreState datastoreState,
        string propertyName)
    {
        switch (propertyName)
        {
            case nameof(ChildContainer.BackColor):
                containerNode.NameBackgroundColor = container.BackColor ?? BlockNodeColors.BackgroundDefault;
                break;
            case nameof(FunctionBlock.Engine):
                containerNode.SetEngineDisplayText(EngineDisplayText.Get(datastoreState.Builder, container));
                break;
            case nameof(ChildContainer.ForeColor):
                containerNode.NameForeColor = container.ForeColor ?? BlockNodeColors.ForegroundDefault;
                break;
            case nameof(ChildContainer.X):
            case nameof(ChildContainer.Y):
                containerNode.SetPosition(container.X ?? 0, container.Y ?? 0);
                break;
            case nameof(ChildContainer.Name):
                containerNode.Name = container.Name;
                break;
        }

        containerNode.RefreshAll();
    }

    internal static void ReloadConnectors(
        ComparerService comparerService,
        ChildContainer container,
        ChildContainerNode containerNode,
        IDatastoreState datastoreState,
        DiagramService diagramService)
    {
        containerNode.Connectors.Clear();
        foreach (var port in containerNode.Ports.ToArray())
            containerNode.RemovePort(port);

        containerNode.Connectors.AddRange(GenerateConnectors(containerNode, container, comparerService, datastoreState, diagramService));
        containerNode.InvalidateConnectorsCache();
        containerNode.CalculateDisplayAllConnectors();

        containerNode.UpdateSize();

        datastoreState.DataflowDiagramMapping.Remove(container);
        datastoreState.DataflowDiagramMapping.Add(container, containerNode);
    }

    internal static void UpdatePosition(IDatastoreState datastoreState, ChildContainerNode containerNode)
    {
        var container = datastoreState.DataflowDiagramMapping.GetModel(containerNode);
        datastoreState.Builder.Editors.Container.SetLocation(
            container,
            new((int)containerNode.Position.X, (int)containerNode.Position.Y));
    }
}
