using System;
using Blazor.Diagrams.Core;
using Microsoft.Extensions.Logging;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.ComponentStates;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;

namespace ViciOne.Ui.ClusterEditor.Services.ComponentServices;

public sealed class DiagramService : IDisposable
{
    private readonly IDatastore _datastore;
    private readonly DiagramEventService _diagramEventService;
    private readonly ILogger<DiagramService> _logger;

    public Diagram Diagram { get; set; } = default!;
    public DiagramState DiagramState { get; } = new();
    public BlockNodeLink? DraggingLink { get; set; }

    public DiagramService(IDatastore datastore, DiagramEventService diagramEventService, ILogger<DiagramService> logger)
    {
        _datastore = datastore;
        _diagramEventService = diagramEventService;
        _logger = logger;

        _datastore.ConnectorLinkRemoved += OnConnectorLinkRemoved;
    }

    public void Dispose()
        => _datastore.ConnectorLinkRemoved -= OnConnectorLinkRemoved;

    private void InvokeDraggingLinkChanged()
    {
        if (DraggingLink is null)
        {
            _diagramEventService.InvokeDraggingLinkChanged(_datastore, null);
            _diagramEventService.RequestEdgeDraggingVisibilityChange(false);
        }
        else
        {
            var draggingLinkSourceNode = (BlockNode)DraggingLink.SourceNode;
            var sourcePortId = DraggingLink.SourcePort!.Id;

            BlockNodeConnector? sourceNodeConnector = null;
            foreach (var connector in draggingLinkSourceNode.ConnectorsToList())
            {
                if (connector.Id == sourcePortId)
                {
                    sourceNodeConnector = connector;
                    break;
                }
            }

            var sourceModel = _datastore.DataflowDiagramMapping.GetModel(sourceNodeConnector!);

            _diagramEventService.InvokeDraggingLinkChanged(_datastore, sourceModel);
            _diagramEventService.RequestEdgeDraggingVisibilityChange(true);
        }

        _diagramEventService.InvokeBlockNodesUpdateRequested();
    }

    private void OnConnectorLinkRemoved(BlockNodeLink nodeLink)
        => Diagram.Links.Remove(nodeLink);

    public void RequestGridModeChange(GridMode gridMode)
    {
        if (gridMode == DiagramState.GridMode)
            return;

        DiagramState.GridMode = gridMode;
        _diagramEventService.RequestGridModeChange(gridMode);
    }

    public void RequestMinimapVisibilityChange(bool isVisible)
    {
        if (isVisible == DiagramState.IsMinimapVisible)
            return;

        DiagramState.IsMinimapVisible = isVisible;
        _diagramEventService.RequestMinimapVisibilityChange(isVisible);
    }

    public void RequestPanBehaviorChange(bool useGimpPanBehavior)
    {
        DiagramState.UsesGimpPanBehavior = useGimpPanBehavior;
        _diagramEventService.RequestPanBehaviorChange(useGimpPanBehavior);
    }

    public void RequestSimplifiedViewChange(bool simplifiedView)
    {
        if (simplifiedView == DiagramState.SimplifiedView)
            return;

        DiagramState.SimplifiedView = simplifiedView;

        if (Diagram is null)
            return;

        foreach (var node in Diagram.Nodes)
            node.Refresh();
    }

    public void SetDraggingLink(BlockNodeLink? link)
    {
        if (DraggingLink == link)
            return;

        if (link is null)
        {
            DraggingLink = null;
            InvokeDraggingLinkChanged();
        }
        else
        {
            DraggingLink = link;
        }
    }

    public void SetDraggingLinkActive()
    {
        if (DraggingLink is null)
            return;

        InvokeDraggingLinkChanged();
    }

    public void SetMinimapNodeColoring(bool isEnabled)
    {
        if (isEnabled == DiagramState.UseNodeColoringOnMinimap)
            return;

        DiagramState.UseNodeColoringOnMinimap = isEnabled;
        _diagramEventService.InvokeMinimapColoringChanged();
    }

    public void SetNodeAlignmentBorderActive(bool isEnabled)
    {
        if (isEnabled == DiagramState.IsNodeAlignmentBorderEnabled)
            return;

        if (!isEnabled)
            SetNodeAlignmentBorderVisibility(false);

        DiagramState.IsNodeAlignmentBorderEnabled = isEnabled;
    }

    public void SetNodeAlignmentBorderVisibility(bool isVisible)
    {
        if (isVisible == DiagramState.IsNodeAlignmentBorderVisible || !DiagramState.IsNodeAlignmentBorderEnabled)
            return;

        DiagramState.IsNodeAlignmentBorderVisible = isVisible;
        AsyncGuard.SafeFireAndForget(() => _diagramEventService.InvokeNodeAlignmentBorderVisibilityChanged(isVisible), _logger);
    }

    public void SetZoom(double newZoom)
    {
        if (newZoom == DiagramState.Zoom)
            return;

        DiagramState.Zoom = newZoom;
        _diagramEventService.InvokeZoomChanged(DiagramState.Zoom);
    }
}
