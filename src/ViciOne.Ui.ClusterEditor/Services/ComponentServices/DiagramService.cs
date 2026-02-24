using System;
using System.Linq;
using Blazor.Diagrams.Core;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.ComponentStates;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;

namespace ViciOne.Ui.ClusterEditor.Services.ComponentServices;

public sealed class DiagramService : IDisposable
{
    private readonly Datastore _datastore;
    private readonly DiagramEventService _diagramEventService;

    public Diagram Diagram { get; set; } = null!;
    public DiagramState DiagramState { get; } = new();
    public BlockNodeLink? DraggingLink { get; set; }

    public DiagramService(Datastore datastore, DiagramEventService diagramEventService)
    {
        _datastore = datastore;
        _diagramEventService = diagramEventService;

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
            var sourceNodeConnector = draggingLinkSourceNode.ConnectorsToList()
                .First(c => c.Id == DraggingLink.SourcePort!.Id);

            var sourceModel = _datastore.DataflowDiagramMapping.GetModel(sourceNodeConnector);

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
        _diagramEventService.RequestSimplifiedViewChange(simplifiedView);
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
        _diagramEventService.InvokeNodeAlignmentBorderVisibilityChanged(isVisible);
    }

    public void SetZoom(double newZoom)
    {
        if (newZoom == DiagramState.Zoom)
            return;

        DiagramState.Zoom = newZoom;
        _diagramEventService.InvokeZoomChanged(DiagramState.Zoom);
    }
}
