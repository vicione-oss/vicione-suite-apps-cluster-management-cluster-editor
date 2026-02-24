using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed class TraceService(ClusterBuilderEventBuffer clusterBuilderEventBuffer, Datastore datastore, DiagramEventService diagramEventService, DiagramService diagramService, SelectionManager selectionManager) : IDisposable
{
    private TraceOptions? _lastTraceOptions;

    private void ClearLinkTraceMarker()
    {
        diagramService.DiagramState.SuppressEvents = true;
        HashSet<BlockNode> nodesToUpdate = [];

        var tracedPublishedConnectors = datastore.DataflowDiagramMapping.GetNodeConnectors().Where(c => c.PublishedConnectorMarker.Traced);
        foreach (var connector in tracedPublishedConnectors)
        {
            connector.PublishedConnectorMarker.Traced = false;
            nodesToUpdate.Add(connector.Node);
        }

        var tracedDataPortConnectors = datastore.DataflowDiagramMapping.GetNodeConnectors().Where(c => c.DataPortConnectorMarker.Traced);
        foreach (var connector in tracedDataPortConnectors)
        {
            connector.DataPortConnectorMarker.Traced = false;
            nodesToUpdate.Add(connector.Node);
        }

        foreach (var node in nodesToUpdate)
            node.Refresh();

        var tracedLinks = datastore.DataflowDiagramMapping.GetNodeLinks().Where(nl => nl.Traced);
        foreach (var link in tracedLinks)
        {
            link.SetTraced(false);
            link.Refresh();
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

        if (!options.BlockNodes.Any())
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
