using System;
using System.Collections.Generic;
using System.Timers;
using Blazor.Diagrams.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components;

public sealed partial class EdgeDraggingArea : ComponentBase, IDisposable
{
    private const int PanValue = 100;

    private readonly Timer _diagramPanTimer = new()
    {
        AutoReset = true,
        Enabled = false,
        Interval = 100,
    };
    private bool _edgeDraggingAreaEnabled;
    private MouseEventArgs? _lastMouseMoveEventArgs;
    private Side? _pointerOverTriggerSide;
    private readonly List<Side> _visibleTriggerSides = [];

    [CascadingParameter] internal Diagram? Diagram { get; set; }

    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;

    private void Cleanup()
    {
        _lastMouseMoveEventArgs = null;
        _pointerOverTriggerSide = null;
        _visibleTriggerSides.Clear();
    }

    public void Dispose()
    {
        _diagramPanTimer.Elapsed -= OnPanTimerElapsed;
        _diagramPanTimer.Dispose();

        DiagramEventService.EdgeDraggingVisibilityChangeRequested -= OnEdgeDraggingVisibilityChangeRequestedAsync;

        GC.SuppressFinalize(this);
    }

    private void OnContainerPointerMove(MouseEventArgs e)
    {
        _lastMouseMoveEventArgs = e;
        DiagramEventService.InvokeEdgeDraggingPointerMove(e);
    }

    private void OnContainerPointerUp(MouseEventArgs e)
    {
        _diagramPanTimer.Stop();
        Cleanup();
        DiagramEventService.InvokeEdgeDraggingPointerUp(e);
    }

    private async void OnEdgeDraggingVisibilityChangeRequestedAsync(bool enabled)
    {
        if (enabled == _edgeDraggingAreaEnabled)
            return;

        _edgeDraggingAreaEnabled = enabled;
        await InvokeAsync(StateHasChanged);
    }

    private void OnIndicatorAreaPointerEnter(Side side)
    {
        if (side is Side.Left or Side.LowerLeft or Side.UpperLeft)
            _visibleTriggerSides.Add(Side.Left);

        if (side is Side.Top or Side.UpperLeft or Side.UpperRight)
            _visibleTriggerSides.Add(Side.Top);

        if (side is Side.Right or Side.LowerRight or Side.UpperRight)
            _visibleTriggerSides.Add(Side.Right);

        if (side is Side.Bottom or Side.LowerLeft or Side.LowerRight)
            _visibleTriggerSides.Add(Side.Bottom);

        if (side is Side.Left or Side.Top or Side.UpperLeft)
            _visibleTriggerSides.Add(Side.UpperLeft);

        if (side is Side.Top or Side.Right or Side.UpperRight)
            _visibleTriggerSides.Add(Side.UpperRight);

        if (side is Side.Right or Side.Bottom or Side.LowerRight)
            _visibleTriggerSides.Add(Side.LowerRight);

        if (side is Side.Bottom or Side.Left or Side.LowerLeft)
            _visibleTriggerSides.Add(Side.LowerLeft);
    }

    private void OnIndicatorAreaPointerLeave()
        => Cleanup();

    protected override void OnInitialized()
    {
        ArgumentNullException.ThrowIfNull(Diagram, nameof(Diagram));

        _diagramPanTimer.Elapsed += OnPanTimerElapsed;
        DiagramEventService.EdgeDraggingVisibilityChangeRequested += OnEdgeDraggingVisibilityChangeRequestedAsync;
    }

    private void OnPanTimerElapsed(object? _1, ElapsedEventArgs _2)
        => InvokeAsync(() => Diagram!.Batch(() =>
            {
                if (!_pointerOverTriggerSide.HasValue)
                    return;

                if (_pointerOverTriggerSide is Side.Top or Side.UpperLeft or Side.UpperRight)
                    Diagram!.UpdatePan(0, PanValue * Diagram.Zoom);
                else if (_pointerOverTriggerSide is Side.Bottom or Side.LowerLeft or Side.LowerRight)
                    Diagram!.UpdatePan(0, -(PanValue * Diagram.Zoom));

                if (_pointerOverTriggerSide is Side.Left or Side.UpperLeft or Side.LowerLeft)
                    Diagram!.UpdatePan(PanValue * Diagram.Zoom, 0);
                else if (_pointerOverTriggerSide is Side.Right or Side.UpperRight or Side.LowerRight)
                    Diagram!.UpdatePan(-(PanValue * Diagram.Zoom), 0);

                if (_lastMouseMoveEventArgs is not null)
                    DiagramEventService.InvokeEdgeDraggingPointerMove(_lastMouseMoveEventArgs);
            })
        );

    private void OnTriggerAreaPointerEnter(Side side)
    {
        _pointerOverTriggerSide = side;
        _diagramPanTimer.Start();
    }

    private void OnTriggerAreaPointerLeave()
    {
        _diagramPanTimer.Stop();
        _pointerOverTriggerSide = null;
    }

    private void OnWheel(WheelEventArgs e)
        => DiagramEventService.InvokeEdgeDraggingWheel(e);

    private enum Side
    {
        Left,
        UpperLeft,
        Top,
        UpperRight,
        Right,
        LowerRight,
        Bottom,
        LowerLeft,
    }
}
