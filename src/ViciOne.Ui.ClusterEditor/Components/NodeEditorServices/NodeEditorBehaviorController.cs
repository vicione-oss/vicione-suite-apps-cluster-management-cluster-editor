using System;
using System.Diagnostics.CodeAnalysis;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Behaviors;
using Microsoft.JSInterop;
using ViciOne.Ui.ClusterEditor.Behaviors;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components.NodeEditorServices;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class NodeEditorBehaviorController(
    IDatastore datastore,
    ClusterBuilderEventBuffer clusterBuilderEventBuffer,
    DiagramEventService diagramEventService,
    DiagramService diagramService,
    InputEventService inputEventService,
    IJSRuntime jsRuntime,
    SelectionManager selectionManager) : IDisposable
{
    private IPanBehavior? _activePanBehavior;
    private BlazorDiagram? _diagram;
    private VODragMovablesBehavior? _dragMovablesBehavior;
    private VODragNewLinkBehavior? _dragNewLinkBehavior;
    private GimpPanBehavior? _gimpPanBehavior;
    private GimpZoomBehavior? _gimpZoomBehavior;
    private bool _initialized;
    private VOKeyboardBehavior? _keyboardBehavior;
    private VOSelectionBehavior? _selectionBehavior;
    private VOPanBehavior? _vOPanBehavior;
    private VOZoomBehavior? _vOZoomBehavior;
    private VOZoomToFitBehavior? _zoomToFitBehavior;

    public bool IsMoving => _dragMovablesBehavior?.IsMoving ?? false;

    public void Dispose()
        => Teardown();

    public void EndGhostMove(double clientX, double clientY)
        => _dragMovablesBehavior!.EndMove(clientX, clientY);

    public void ExternalGhostMove(double clientX, double clientY)
        => _dragMovablesBehavior!.ExternalMove(clientX, clientY);

    public void Initialize(BlazorDiagram diagram)
    {
        if (_initialized)
            Teardown();

        _diagram = diagram;

        _diagram.UnregisterBehavior<PanBehavior>();
        _diagram.UnregisterBehavior<ZoomBehavior>();
        _diagram.UnregisterBehavior<DragNewLinkBehavior>();

        // Um das Default SelectionBehavior austauschen zu können, muss man das DragMovablesBehavior neu binden,
        // ansonsten kommt es zu einer Exception beim Bewegen von Elementen. Diese beiden Behaviors scheinen intern
        // voneinander abzuhängen.
        _diagram.UnregisterBehavior<DragMovablesBehavior>();
        _diagram.UnregisterBehavior<SelectionBehavior>();
        _diagram.UnregisterBehavior<KeyboardShortcutsBehavior>();

        _selectionBehavior = new(_diagram, selectionManager);
        _diagram.RegisterBehavior(_selectionBehavior);
        _dragMovablesBehavior = new(datastore, _diagram, diagramEventService, diagramService, inputEventService, jsRuntime);
        _diagram.RegisterBehavior(_dragMovablesBehavior);

        _dragNewLinkBehavior = new(datastore, _diagram, diagramEventService, diagramService, inputEventService);
        _diagram.RegisterBehavior(_dragNewLinkBehavior);
        _zoomToFitBehavior = new(_diagram);
        _diagram.RegisterBehavior(_zoomToFitBehavior);

        _keyboardBehavior = new(datastore, clusterBuilderEventBuffer, _diagram, diagramEventService, diagramService);
        _diagram.RegisterBehavior(_keyboardBehavior);

        // Subscribe before requesting the initial pan behavior change so the Gimp/VO pan+zoom
        // behaviors actually get registered by OnPanBehaviorChangeRequested.
        diagramEventService.PanBehaviorChangeRequested += OnPanBehaviorChangeRequested;
        diagramEventService.ZoomToFitRequested += OnZoomToFitRequested;
        diagramEventService.DiagramPointerLeave += OnDiagramPointerLeave;

        if (diagramService.DiagramState.UsesGimpPanBehavior is null)
            diagramService.RequestPanBehaviorChange(false);
        else
            diagramService.RequestPanBehaviorChange(diagramService.DiagramState.UsesGimpPanBehavior.GetValueOrDefault());

        _initialized = true;
    }

    private void OnDiagramPointerLeave()
        => _activePanBehavior?.StopPointerMove();

    internal void OnPanBehaviorChangeRequested(bool useGimpPanBehavior)
    {
        if (useGimpPanBehavior)
        {
            if (_gimpPanBehavior is not null)
                return;

            _diagram!.UnregisterBehavior<VOPanBehavior>();
            _vOPanBehavior?.Dispose();
            _vOPanBehavior = null;
            _gimpPanBehavior = new(_diagram, diagramEventService);
            _diagram.RegisterBehavior(_gimpPanBehavior);

            _diagram.UnregisterBehavior<VOZoomBehavior>();
            _vOZoomBehavior?.Dispose();
            _vOZoomBehavior = null;
            _gimpZoomBehavior = new(_diagram, diagramEventService);
            _diagram.RegisterBehavior(_gimpZoomBehavior);

            _activePanBehavior = _gimpPanBehavior;
        }
        else
        {
            if (_vOPanBehavior is not null)
                return;

            _diagram!.UnregisterBehavior<GimpPanBehavior>();
            _gimpPanBehavior?.Dispose();
            _gimpPanBehavior = null;
            _vOPanBehavior = new(_diagram, diagramEventService);
            _diagram.RegisterBehavior(_vOPanBehavior);

            _diagram.UnregisterBehavior<GimpZoomBehavior>();
            _gimpZoomBehavior?.Dispose();
            _gimpZoomBehavior = null;
            _vOZoomBehavior = new(_diagram, diagramEventService);
            _diagram.RegisterBehavior(_vOZoomBehavior);

            _activePanBehavior = _vOPanBehavior;
        }
    }

    private void OnZoomToFitRequested()
        => _zoomToFitBehavior!.ZoomToFit();

    public void StartGhostMove(double clientX, double clientY)
        => _dragMovablesBehavior!.Start(clientX, clientY, useJsDomEvents: false);

    private void Teardown()
    {
        diagramEventService.PanBehaviorChangeRequested -= OnPanBehaviorChangeRequested;
        diagramEventService.ZoomToFitRequested -= OnZoomToFitRequested;
        diagramEventService.DiagramPointerLeave -= OnDiagramPointerLeave;

        if (_diagram is not null)
        {
            _diagram.UnregisterBehavior<VOSelectionBehavior>();
            _diagram.UnregisterBehavior<VODragMovablesBehavior>();
            _diagram.UnregisterBehavior<VODragNewLinkBehavior>();
            _diagram.UnregisterBehavior<VOZoomToFitBehavior>();
            _diagram.UnregisterBehavior<VOKeyboardBehavior>();
            _diagram.UnregisterBehavior<GimpPanBehavior>();
            _diagram.UnregisterBehavior<GimpZoomBehavior>();
            _diagram.UnregisterBehavior<VOPanBehavior>();
            _diagram.UnregisterBehavior<VOZoomBehavior>();
        }

        _dragMovablesBehavior?.Dispose();
        _dragNewLinkBehavior?.Dispose();
        _gimpPanBehavior?.Dispose();
        _gimpZoomBehavior?.Dispose();
        _keyboardBehavior?.Dispose();
        _selectionBehavior?.Dispose();
        _vOPanBehavior?.Dispose();
        _vOZoomBehavior?.Dispose();
        _zoomToFitBehavior?.Dispose();

        _dragMovablesBehavior = null;
        _dragNewLinkBehavior = null;
        _gimpPanBehavior = null;
        _gimpZoomBehavior = null;
        _keyboardBehavior = null;
        _selectionBehavior = null;
        _vOPanBehavior = null;
        _vOZoomBehavior = null;
        _zoomToFitBehavior = null;
        _activePanBehavior = null;
        _diagram = null;
        _initialized = false;
    }
}
