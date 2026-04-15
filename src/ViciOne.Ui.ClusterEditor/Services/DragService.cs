using System;
using System.Collections.Generic;
using System.Linq;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models.Base;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Services;

internal sealed class DragService(
    DiagramEventService diagramEventService,
    DiagramService diagramService,
    InputEventService inputEventService)
{
    private Point? _lastPosition;

    internal IEnumerable<IDragable> DraggedItems { get; private set; } = [];
    internal IEnumerable<IDragTarget>? DragTargets { get; private set; }

    internal event Action<Point>? DraggingEnded;
    internal event Action<Point>? DraggingPositionChanged;
    internal event Action? DraggingStarted;

    internal void EndDragging()
    {
        diagramEventService.EdgeDraggingPointerUp -= OnPointerUp;
        diagramService.Diagram.PointerUp -= OnDiagramPointerUp;
        inputEventService.KeyDown -= OnKeyDown;
        inputEventService.PointerMove -= OnPointerMove;
        inputEventService.PointerUp -= OnPointerUp;

        HighlightDragTargets(false);

        if (_lastPosition is not null)
            DraggingEnded?.Invoke(_lastPosition);

        _lastPosition = null;
    }

    private void HighlightDragTargets(bool highlight)
    {
        if (DragTargets is null)
            return;

        foreach (var dragTarget in DragTargets)
            dragTarget.HighlightAsTarget(highlight);

        if (DragTargets.Any(dt => dt is BlockNodeConnector))
            diagramEventService.InvokeBlockNodesUpdateRequested();
    }

    private void OnDiagramPointerUp(Model? _, global::Blazor.Diagrams.Core.Events.PointerEventArgs _2)
        => EndDragging();

    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Code == KeyboardCodes.Escape)
            EndDragging();
    }

    public void OnPointerMove(MouseEventArgs e)
    {
        if (_lastPosition is null)
            HighlightDragTargets(true);

        var current = new Point(e.ClientX, e.ClientY);

        if (_lastPosition is not null)
            DraggingPositionChanged?.Invoke(current);

        _lastPosition = current;
    }

    private void OnPointerUp(PointerEventArgs _)
        => EndDragging();

    internal void StartDragging(IEnumerable<IDragable> draggedItems, IEnumerable<IDragTarget>? dragTargets = null, bool useEvents = true)
    {
        DraggedItems = draggedItems;
        DragTargets = dragTargets ?? [];

        if (useEvents)
        {
            diagramEventService.EdgeDraggingPointerUp += OnPointerUp;
            diagramService.Diagram.PointerUp += OnDiagramPointerUp;
            inputEventService.KeyDown += OnKeyDown;
            inputEventService.PointerMove += OnPointerMove;
            inputEventService.PointerUp += OnPointerUp;
        }

        DraggingStarted?.Invoke();
    }
}
