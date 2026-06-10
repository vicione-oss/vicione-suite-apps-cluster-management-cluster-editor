using System;
using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed class TraceService(ClusterBuilderEventBuffer clusterBuilderEventBuffer, IDatastore datastore, DiagramEventService diagramEventService, DiagramService diagramService, SelectionManager selectionManager) : IDisposable
{
    private TraceOptions? _lastTraceOptions;

    private void ClearLinkTraceMarker()
    {
        diagramService.DiagramState.SuppressEvents = true;
        HashSet<BlockNode> nodesToUpdate = [];

        foreach (var connector in datastore.DataflowDiagramMapping.GetNodeConnectors())
        {
            if (connector.PublishedConnectorMarker.Traced)
            {
                connector.PublishedConnectorMarker.Traced = false;
                nodesToUpdate.Add(connector.Node);
            }

            if (connector.DataPortConnectorMarker.Traced)
            {
                connector.DataPortConnectorMarker.Traced = false;
                nodesToUpdate.Add(connector.Node);
            }
        }

        foreach (var node in nodesToUpdate)
            node.Refresh();

        foreach (var link in datastore.DataflowDiagramMapping.GetNodeLinks())
        {
            if (link.Traced)
            {
                link.SetTraced(false);
                link.Refresh();
            }
        }

        diagramService.DiagramState.SuppressEvents = false;
    }

    public void ClearTrace()
    {
        ClearLinkTraceMarker();

        diagramEventService.ContainerLoaded -= OnContainerLoaded;
        clusterBuilderEventBuffer.ConnectorLinksAdded -= Refresh;
        clusterBuilderEventBuffer.ConnectorLinksRemoved -= Refresh;
        _lastTraceOptions = null;
    }

    public void Dispose()
    {
        diagramEventService.ContainerLoaded -= OnContainerLoaded;
        clusterBuilderEventBuffer.ConnectorLinksAdded -= Refresh;
        clusterBuilderEventBuffer.ConnectorLinksRemoved -= Refresh;
        _lastTraceOptions = null;
    }

    private void OnContainerLoaded(Container _)
        => ClearTrace();

    private void Refresh(IEnumerable<Link> links)
    {
        if (_lastTraceOptions is not null)
            TraceConnections(_lastTraceOptions);
    }

    public void TraceConnections(ConnectionDirection direction, int? depth)
    {
        var options = new TraceOptions
        {
            BlockNodes = selectionManager.SelectedBlockNodes,
            ConnectionDirection = direction,
            Depth = depth
        };

        _lastTraceOptions = options;

        diagramEventService.ContainerLoaded += OnContainerLoaded;
        clusterBuilderEventBuffer.ConnectorLinksAdded += Refresh;
        clusterBuilderEventBuffer.ConnectorLinksRemoved += Refresh;

        TraceConnections(options);
    }

    private void TraceConnections(TraceOptions options)
    {
        ClearLinkTraceMarker();

        if (options.BlockNodes.Count == 0)
            return;

        diagramService.DiagramState.SuppressEvents = true;
        var nodesToUpdate = new HashSet<BlockNode>();

        foreach (var node in options.BlockNodes)
        {
            var connectedLinks = datastore.GetConnectedLinks(node, options.ConnectionDirection, options.Depth);

            foreach (var link in connectedLinks)
            {
                if (link.Visible)
                {
                    // Regular link
                    if (!datastore.DataflowDiagramMapping.TryGetDiagramModel(link, out var linkModel))
                        continue;

                    linkModel.SetTraced(true);
                    linkModel.Refresh();
                }
                else
                {
                    // Published or DataPort link
                    var connectors = datastore.DataflowDiagramMapping.GetNodeConnectors();
                    foreach (var connector in connectors)
                    {
                        if (connector.PublishedConnectorMarker.Links.Contains(link))
                        {
                            connector.PublishedConnectorMarker.Traced = true;
                            nodesToUpdate.Add(connector.Node);
                        }

                        if (connector.DataPortConnectorMarker.Links.Contains(link))
                        {
                            connector.DataPortConnectorMarker.Traced = true;
                            nodesToUpdate.Add(connector.Node);
                        }
                    }
                }
            }
        }

        foreach (var node in nodesToUpdate)
            node.Refresh();

        diagramService.DiagramState.SuppressEvents = false;
    }
}
