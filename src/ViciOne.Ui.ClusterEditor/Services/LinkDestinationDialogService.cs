using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed class LinkDestinationDialogService(IDatastore datastore)
{
    public IReadOnlyList<DataGridConnectorWrapper> ConnectorWrappers { get; private set; } = [];
    public IReadOnlyList<DataGridDataPortWrapper> DataPortWrappers { get; private set; } = [];
    public bool IsDeletionMode { get; set; }
    public ConnectorMarker? SourceConnectorMarker { get; private set; }
    public DataPortTreeNode? SourceDataPortTreeNode { get; private set; }
    public bool Visible { get; private set; }

    public event Action<Connector, ConnectorMarkerType>? ConnectorSelected;
    public event Action<DataPortTreeNode>? DataPortTreeNodeSelected;
    public event Action<IReadOnlyList<Link>>? LinksToDeleteSelected;
    public event Action? VisibilityChanged;

    public void InvokeConnectorSelected(Connector connector, ConnectorMarkerType marker)
        => ConnectorSelected?.Invoke(connector, marker);

    public void InvokeDataPortSelected(DataPortTreeNode dataPortTreeNode)
        => DataPortTreeNodeSelected?.Invoke(dataPortTreeNode);

    public void InvokeLinksToDeleteSelected(IEnumerable<Connector> connectors)
    {
        if (SourceConnectorMarker is null)
            return;

        var connectorSet = new HashSet<Connector>(connectors);
        List<Link> result = [];

        foreach (var link in SourceConnectorMarker.Links)
        {
            Connector? target = SourceConnectorMarker.Connector.IsInput
                ? link.SourceConnector
                : link.DestinationConnector;

            if (target is not null && connectorSet.Contains(target))
                result.Add(link);
        }

        LinksToDeleteSelected?.Invoke(result);
    }

    public void InvokeLinksToDeleteSelected(IEnumerable<DataPortTreeNode> dataPortTreeNodes)
    {
        if (SourceConnectorMarker is null)
            return;

        var nodeSet = new HashSet<DataPortTreeNode>(dataPortTreeNodes);
        List<Link> result = [];

        foreach (var link in SourceConnectorMarker.Links)
        {
            var target = SourceConnectorMarker.Connector.IsInput
                ? link.SourceDataPortTreeNode
                : link.DestinationDataPortTreeNode;

            if (target is not null && nodeSet.Contains(target))
                result.Add(link);
        }

        LinksToDeleteSelected?.Invoke(result);
    }

    public void SetSourceConnectorMarker(ConnectorMarker? sourceConnectorMarker)
    {
        ConnectorWrappers = [];
        DataPortWrappers = [];
        SourceDataPortTreeNode = null;

        if (sourceConnectorMarker is null || sourceConnectorMarker.Links.Count < 1)
        {
            SourceConnectorMarker = null;
            return;
        }

        if (sourceConnectorMarker.Links[0].SourceConnector is null || sourceConnectorMarker.Links[0].DestinationConnector is null)
        {
            List<DataGridDataPortWrapper> dataPortResult = [];
            foreach (var link in sourceConnectorMarker.Links)
            {
                var node = sourceConnectorMarker.Connector.IsInput
                    ? link.SourceDataPortTreeNode!
                    : link.DestinationDataPortTreeNode!;
                dataPortResult.Add(new DataGridDataPortWrapper(node, node.GetPath(datastore)));
            }

            DataPortWrappers = dataPortResult;
        }
        else
        {
            List<DataGridConnectorWrapper> connectorResult = [];
            foreach (var link in sourceConnectorMarker.Links)
            {
                if (sourceConnectorMarker.Connector.IsInput)
                {
                    if (link.SourceConnector is ConnectorOutput)
                    {
                        connectorResult.Add(new DataGridConnectorWrapper(link.SourceConnector!,
                            datastore.Builder.ResolveConnectorDesign(link.SourceConnector!),
                            datastore.Builder.ResolveFunctionBlockDesign(link.SourceConnector!.FunctionBlock.DesignId)));
                    }
                }
                else
                {
                    if (link.DestinationConnector is ConnectorInput)
                    {
                        connectorResult.Add(new DataGridConnectorWrapper(link.DestinationConnector!,
                            datastore.Builder.ResolveConnectorDesign(link.DestinationConnector!),
                            datastore.Builder.ResolveFunctionBlockDesign(link.DestinationConnector!.FunctionBlock.DesignId)));
                    }
                }
            }

            ConnectorWrappers = connectorResult;
        }

        SourceConnectorMarker = sourceConnectorMarker;
    }

    public void SetSourceDataPortTreeNode(DataPortTreeNode? sourceDataPortTreeNode)
    {
        ConnectorWrappers = [];
        DataPortWrappers = [];
        SourceConnectorMarker = null;

        if (sourceDataPortTreeNode is null || !sourceDataPortTreeNode.Links.Any())
        {
            SourceDataPortTreeNode = null;
            return;
        }

        List<DataGridConnectorWrapper> result = [];
        foreach (var link in sourceDataPortTreeNode.Links)
        {
            var connector = (Connector?)(link.SourceConnector is null ? link.DestinationConnector : link.SourceConnector);
            if (connector is not null)
            {
                result.Add(new DataGridConnectorWrapper(connector,
                    datastore.Builder.ResolveConnectorDesign(connector),
                    datastore.Builder.ResolveFunctionBlockDesign(connector.FunctionBlock.DesignId),
                    ConnectorMarkerType.DataPort));
            }
        }

        ConnectorWrappers = result;
        SourceDataPortTreeNode = sourceDataPortTreeNode;
    }

    public void SetVisibility(bool visible)
    {
        if (visible != Visible)
        {
            Visible = visible;

            VisibilityChanged?.Invoke();
        }
    }
}
