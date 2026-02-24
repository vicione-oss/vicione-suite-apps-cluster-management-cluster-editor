using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed class LinkDestinationDialogService(Datastore datastore)
{
    public IEnumerable<DataGridConnectorWrapper> ConnectorWrappers { get; private set; } = [];
    public IEnumerable<DataGridDataPortWrapper> DataPortWrappers { get; private set; } = [];
    public bool IsDeletionMode { get; set; }
    public ConnectorMarker? SourceConnectorMarker { get; private set; }
    public DataPortTreeNode? SourceDataPortTreeNode { get; private set; }
    public bool Visible { get; set; }

    public event Action<Connector>? ConnectorSelected;
    public event Action<DataPortTreeNode>? DataPortTreeNodeSelected;
    public event Action<IEnumerable<Link>>? LinksToDeleteSelected;
    public event Action? VisibilityChanged;

    public void InvokeConnectorSelected(Connector connector)
        => ConnectorSelected?.Invoke(connector);

    public void InvokeDataPortSelected(DataPortTreeNode dataPortTreeNode)
        => DataPortTreeNodeSelected?.Invoke(dataPortTreeNode);

    public void InvokeLinksToDeleteSelected(IEnumerable<Connector> connectors)
    {
        if (SourceConnectorMarker is null)
            return;

        var links = SourceConnectorMarker.Connector.IsInput
            ? SourceConnectorMarker.Links.Where(l => connectors.Contains(l.SourceConnector))
            : SourceConnectorMarker.Links.Where(l => connectors.Contains(l.DestinationConnector));

        LinksToDeleteSelected?.Invoke(links);
    }

    public void InvokeLinksToDeleteSelected(IEnumerable<DataPortTreeNode> dataPortTreeNodes)
    {
        if (SourceConnectorMarker is null)
            return;

        var links = SourceConnectorMarker.Connector.IsInput
            ? SourceConnectorMarker.Links.Where(l => dataPortTreeNodes.Contains(l.SourceDataPortTreeNode))
            : SourceConnectorMarker.Links.Where(l => dataPortTreeNodes.Contains(l.DestinationDataPortTreeNode));

        LinksToDeleteSelected?.Invoke(links);
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
            DataPortWrappers = sourceConnectorMarker.Connector.IsInput
                ? sourceConnectorMarker.Links
                    .Select(l => new DataGridDataPortWrapper(l.SourceDataPortTreeNode!, l.SourceDataPortTreeNode!.GetPath(datastore)))
                : sourceConnectorMarker.Links
                    .Select(l => new DataGridDataPortWrapper(l.DestinationDataPortTreeNode!, l.DestinationDataPortTreeNode!.GetPath(datastore)));
        }
        else
        {
            ConnectorWrappers = sourceConnectorMarker.Connector.IsInput
                ? sourceConnectorMarker.Links
                    .Where(l => (l.SourceConnector is ConnectorOutput output) && output is not null)
                    .Select(l => new DataGridConnectorWrapper(l.SourceConnector!,
                        datastore.Builder.ResolveConnectorDesign(l.SourceConnector!),
                        datastore.Builder.ResolveFunctionBlockDesign(l.SourceConnector!.FunctionBlock.DesignId)))
                : sourceConnectorMarker.Links
                    .Where(l => (l.DestinationConnector is ConnectorInput input) && input is not null)
                    .Select(l => new DataGridConnectorWrapper(l.DestinationConnector!,
                        datastore.Builder.ResolveConnectorDesign(l.DestinationConnector!),
                        datastore.Builder.ResolveFunctionBlockDesign(l.DestinationConnector!.FunctionBlock.DesignId)));
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

        ConnectorWrappers = [.. sourceDataPortTreeNode.Links
            .Select(link => (Connector)(link.SourceConnector is null ? link.DestinationConnector : link.SourceConnector)!)
            .Where(connector => connector is not null)
            .Select(connector => new DataGridConnectorWrapper(connector!,
                datastore.Builder.ResolveConnectorDesign(connector!),
                datastore.Builder.ResolveFunctionBlockDesign(connector!.FunctionBlock.DesignId)))];

        SourceDataPortTreeNode = sourceDataPortTreeNode;
    }

    public void SetVisibility(bool visible)
    {
        Visible = visible;
        VisibilityChanged?.Invoke();
    }
}
