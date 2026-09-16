using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Blazor.Diagrams.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components;

public sealed partial class EdgeDraggingArea : ComponentBase, IDisposable
{
    private const int PanValue = 100;

    private bool _edgeDraggingAreaEnabled;
    private PointerEventArgs? _lastPointerMoveEventArgs;
    private CancellationTokenSource? _panCts;
    private Side? _pointerOverTriggerSide;
    private readonly List<Side> _visibleTriggerSides = [];

    [CascadingParameter] internal Diagram? Diagram { get; set; }

    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;

    private void Cleanup()
    {
        _lastPointerMoveEventArgs = null;
        _pointerOverTriggerSide = null;
        _visibleTriggerSides.Clear();
    }

    public void Dispose()
    {
        var oldCts = Interlocked.Exchange(ref _panCts, null);
        oldCts?.Cancel();
        oldCts?.Dispose();
        DiagramEventService.EdgeDraggingVisibilityChangeRequested -= OnEdgeDraggingVisibilityChangeRequested;
    }

    private void OnContainerPointerCancel(PointerEventArgs _)
    {
        StopPanning();
        Cleanup();
    }

    private void OnContainerPointerMove(PointerEventArgs e)
    {
        _lastPointerMoveEventArgs = e;
        DiagramEventService.InvokeEdgeDraggingPointerMove(e);
    }

    private void OnContainerPointerUp(PointerEventArgs e)
    {
        StopPanning();
        Cleanup();
        DiagramEventService.InvokeEdgeDraggingPointerUp(e);
    }

    private async void OnEdgeDraggingVisibilityChangeRequested(bool enabled)
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
        DiagramEventService.EdgeDraggingVisibilityChangeRequested += OnEdgeDraggingVisibilityChangeRequested;
    }

    private void OnTriggerAreaPointerEnter(Side side)
    {
        _pointerOverTriggerSide = side;
        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _panCts, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();
        _ = PanRepeatedly(newCts.Token);
    }

    private void OnTriggerAreaPointerLeave()
    {
        StopPanning();
        _pointerOverTriggerSide = null;
    }

    private void OnWheel(WheelEventArgs e)
        => DiagramEventService.InvokeEdgeDraggingWheel(e);

    private async Task PanRepeatedly(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(100, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

            if (cancellationToken.IsCancellationRequested)
                return;

            await InvokeAsync(() => Diagram!.Batch(() =>
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

                if (_lastPointerMoveEventArgs is not null)
                    DiagramEventService.InvokeEdgeDraggingPointerMove(_lastPointerMoveEventArgs);
            }));
        }
    }

    private void StopPanning()
    {
        var oldCts = Interlocked.Exchange(ref _panCts, null);
        oldCts?.Cancel();
        oldCts?.Dispose();
    }

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
