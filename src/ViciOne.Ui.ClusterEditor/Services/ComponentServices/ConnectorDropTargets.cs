using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.Ui.Blazor.Components.Draggable.Services;
using ViciOne.Ui.ClusterEditor.Components.DiagramComponents;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;

namespace ViciOne.Ui.ClusterEditor.Services.ComponentServices;

/// <summary>
/// Admits the connectors of the diagram to a drag and marks the admitted ones as drop targets, all of them in one
/// pass, so the blocks update once per drag start and once per drag end, not once per connector.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class ConnectorDropTargets : IDisposable
{
    private readonly DiagramEventService _diagramEventService;
    private readonly IDragInteraction _dragInteraction;
    private readonly IDropPolicy<BlockNodeConnector> _dropPolicy;

    // In render order, which the drag interaction turns into the z-order it hit-tests the dropzones in.
    private readonly List<ConnectorDropzone> _dropzones = [];
    private readonly List<BlockNodeConnector> _markedConnectors = [];

    public ConnectorDropTargets(DiagramEventService diagramEventService, IDragInteraction dragInteraction,
        IDropPolicy<BlockNodeConnector> dropPolicy)
    {
        _diagramEventService = diagramEventService;
        _dragInteraction = dragInteraction;
        _dropPolicy = dropPolicy;

        _dragInteraction.DragStart += DragInteractionDragStart;
    }

    /// <summary>
    /// Clears every mark the current drag has set.
    /// </summary>
    /// <remarks>
    /// The drag interaction ends the drag on each of its dropzones; only the first call finds marks to clear.
    /// </remarks>
    public void ClearMarks()
    {
        if (UnmarkAll())
            _diagramEventService.InvokeBlockNodesUpdateRequested();
    }

    public void Dispose()
        => _dragInteraction.DragStart -= DragInteractionDragStart;

    public void Register(ConnectorDropzone dropzone)
        => _dropzones.Add(dropzone);

    public void Unregister(ConnectorDropzone dropzone)
    {
        _dropzones.Remove(dropzone);

        // Nothing else clears the mark once the dropzone is gone, and a marked input port is an unlocked one,
        // so a block removed mid-drag would leave the port permanently attachable.
        if (!_markedConnectors.Remove(dropzone.Connector))
            return;

        dropzone.Connector.SetIsValidDropTarget(false);
        _diagramEventService.InvokeBlockNodesUpdateRequested();
    }

    private void DragInteractionDragStart(object? sender, DragStartEventArgs args)
    {
        // The drag interaction reports no end when pointer capture is lost, so an interrupted drag can leave its
        // marks standing.
        var marksChanged = UnmarkAll();

        foreach (var dropzone in _dropzones)
        {
            if (!_dropPolicy.Accepts(args.Draggable, dropzone.Connector))
                continue;

            args.Dropzones.Add(dropzone);

            dropzone.Connector.SetIsValidDropTarget(true);
            _markedConnectors.Add(dropzone.Connector);
            marksChanged = true;
        }

        // A single update for all marks: every block scans its connectors on each update, and a block re-renders
        // only when it gains or loses its first drop target, so updating per mark would cost a scan of every block
        // per connector and leave the later marked connectors of a block hidden in Simplified View.
        if (marksChanged)
            _diagramEventService.InvokeBlockNodesUpdateRequested();
    }

    private bool UnmarkAll()
    {
        if (_markedConnectors.Count == 0)
            return false;

        foreach (var connector in _markedConnectors)
            connector.SetIsValidDropTarget(false);

        _markedConnectors.Clear();

        return true;
    }
}
