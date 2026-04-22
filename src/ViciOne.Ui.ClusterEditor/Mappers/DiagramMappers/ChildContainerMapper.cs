using System;
using System.Collections.Generic;
using System.Linq;
using Blazor.Diagrams.Core.Geometry;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Helpers;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.Shared.Dx.Services;

namespace ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;

internal static class ChildContainerMapper
{
    internal static ChildContainerNode CreateNode(
        ComparerService comparerService,
        IDatastore datastore,
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

        node.SetEngineDisplayText(EngineDisplayText.Get(datastore.Builder, childContainer));

        var nextLevelElements = GetContainerElementCount(childContainer, true);
        var allElements = GetContainerElementCount(childContainer, false);
        node.SetChildrenInformation(nextLevelElements, allElements);

        node.Connectors.AddRange(GenerateConnectors(node, childContainer, comparerService, datastore, diagramService));
        node.CalculateDisplayAllConnectors();

        node.NameFieldHeight = nameFieldHeight;
        node.UpdateSize();
        node.PrecomputePortsInformation();

        return node;
    }

    internal static IEnumerable<BlockNodeConnector?[]> GenerateConnectors(
        ChildContainerNode childContainerNode,
        ChildContainer childContainer,
        ComparerService comparerService,
        IDatastore datastore,
        DiagramService diagramService)
    {
        var result = new List<BlockNodeConnector?[]>();

        for (var i = 0; i < BlockNodeLayout.SystemConnectorRows; i++)
            result.Add(new BlockNodeConnector?[2]);

        var connectors = childContainer.GetConnectors().ToArray();
        var maxRowCount = Math.Max(BlockNodeLayout.MinimumConnectorRows - BlockNodeLayout.SystemConnectorRows,
            connectors.Length != 0 ? connectors.Max(c => c.Index) + 1 : 0);

        for (var i = 0; i < maxRowCount; i++)
        {
            var row = new BlockNodeConnector?[2];
            var inputConnector = connectors.FirstOrDefault(c => c.Index == i && c is ContainerConnectorInput);
            var outputConnector = connectors.FirstOrDefault(c => c.Index == i && c is ContainerConnectorOutput);

            if (inputConnector is not null)
                row[0] = ConnectorMapper.CreateNodeConnector(comparerService, datastore, diagramService, childContainerNode, inputConnector);
            if (outputConnector is not null)
                row[1] = ConnectorMapper.CreateNodeConnector(comparerService, datastore, diagramService, childContainerNode, outputConnector);

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
        IDatastore datastore,
        string propertyName)
    {
        switch (propertyName)
        {
            case nameof(ChildContainer.BackColor):
                containerNode.NameBackgroundColor = container.BackColor ?? BlockNodeColors.BackgroundDefault;
                break;
            case nameof(FunctionBlock.Engine):
                containerNode.SetEngineDisplayText(EngineDisplayText.Get(datastore.Builder, container));
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

    internal static void ReloadConnectorsAsync(
        ComparerService comparerService,
        ChildContainer container,
        ChildContainerNode containerNode,
        IDatastore datastore,
        DiagramService diagramService)
    {
        containerNode.Connectors.Clear();
        foreach (var port in containerNode.Ports.ToArray())
            containerNode.RemovePort(port);

        containerNode.Connectors.AddRange(GenerateConnectors(containerNode, container, comparerService, datastore, diagramService));
        containerNode.CalculateDisplayAllConnectors();

        containerNode.UpdateSize();
        containerNode.PrecomputePortsInformation();

        datastore.DataflowDiagramMapping.Remove(container);
        datastore.DataflowDiagramMapping.Add(container, containerNode);
    }

    internal static void UpdatePosition(IDatastore datastore, ChildContainerNode containerNode)
    {
        var container = datastore.DataflowDiagramMapping.GetModel(containerNode);
        datastore.Builder.Editors.Container.SetLocation(
            container,
            new((int)containerNode.Position.X, (int)containerNode.Position.Y));
    }
}
