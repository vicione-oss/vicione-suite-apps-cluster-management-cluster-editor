using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.ClusterEditor.Helpers;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Templates;
using ViciOne.Ui.TreeEditor.Templates.Fragments.Node;

namespace ViciOne.Ui.ClusterEditor.Components.TreeNodes;

public partial class CustomTooltipTreeNode : NodeTemplate, IAsyncDisposable
{
    private ActionButtonParameters? _actionButtonContainerParameters;
    private DropAreaParameters? _dropAreaParameters;
    private PointerEventArgs? _lastPointerMoveEvents;
    private CancellationTokenSource? _mouseMoveCts;
    private bool _mouseMoveDebounceRunning;
    private readonly object _tooltipKey = new();
    private bool _tooltipVisible;

    [Inject] private BoundsService BoundsService { get; set; } = default!;
    [Inject] private TooltipService TooltipService { get; set; } = default!;

    protected override void Calculate()
    {
        // reset internal drop zone state on refresh if drop got disabled
        if (_dropAreaParameters is not null && !Node.DropZoneActive)
            _dropAreaParameters.CurrentlyOverDropZone = DropZone.None;

        _actionButtonContainerParameters?.CalculateCss();
    }

    private void CancelMouseMoveDebounce()
    {
        var oldCts = Interlocked.Exchange(ref _mouseMoveCts, null);
        oldCts?.Cancel();
        oldCts?.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (_dropAreaParameters is not null)
        {
            _dropAreaParameters.CalculateAllRequested -= CalculateAll;
            _dropAreaParameters.RefreshRequested -= Refresh;
        }

        Node.DragAndDropStateChanged -= OnDragAndDropStateChangedAsync;

        TooltipService.StopTooltip(_tooltipKey);

        var oldCts = Interlocked.Exchange(ref _mouseMoveCts, null);
        oldCts?.Cancel();
        oldCts?.Dispose();
    }

    protected virtual async Task<TooltipInfo> GetTooltipInfo(MouseEventArgs e, Rectangle parentBounds)
        => TooltipSimpleData.GetSimpleTooltipInfo(e, Node.DisplayText, parentBounds);

    private async void OnDragAndDropStateChangedAsync()
    {
        if (_dropAreaParameters is not null && !Node.DropZoneActive)
            _dropAreaParameters.CurrentlyOverDropZone = DropZone.None;

        await RefreshAsync();
    }

    protected override void OnInitialized()
    {
        // this is important because the base class overrides this itself too
        base.OnInitialized();

        _actionButtonContainerParameters = new() { Builder = Builder, Node = Node, };
        _actionButtonContainerParameters.CalculateCss();

        _dropAreaParameters = new() { Builder = Builder, Node = Node, };
        _dropAreaParameters.CalculateAllRequested += CalculateAll;
        _dropAreaParameters.RefreshRequested += Refresh;

        Node.DragAndDropStateChanged += OnDragAndDropStateChangedAsync;
    }

    private void OnNodeTextPointerLeave()
    {
        _lastPointerMoveEvents = null;
        _tooltipVisible = false;
        CancelMouseMoveDebounce();
        TooltipService.StopTooltip(_tooltipKey);
    }

    private void OnNodeTextPointerMove(PointerEventArgs e)
    {
        _lastPointerMoveEvents = e;

        if (_tooltipVisible)
        {
            CancelMouseMoveDebounce();
            return;
        }

        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _mouseMoveCts, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();

        if (_mouseMoveDebounceRunning)
            return;

        _mouseMoveDebounceRunning = true;
        _ = ShowTooltipAfterDelay();
    }

    protected override void OnPointerEnter(PointerEventArgs e)
    {
        if (!Builder.Settings.ActionsVisibility.HasFlag(ActionVisibility.Hover) || _actionButtonContainerParameters is null)
            return;

        _actionButtonContainerParameters.Hovering = true;

        CalculateAll();
        Refresh();
    }

    protected override void OnPointerLeave(PointerEventArgs e)
    {
        if (!Builder.Settings.ActionsVisibility.HasFlag(ActionVisibility.Hover) || _actionButtonContainerParameters is null)
            return;

        _actionButtonContainerParameters.Hovering = false;

        CalculateAll();
        Refresh();
    }

    private async Task ShowTooltipAfterDelay()
    {
        try
        {
            while (true)
            {
                var cts = _mouseMoveCts;
                if (cts is null)
                    return;

                try
                {
                    await Task.Delay(200, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    if (_mouseMoveCts is null)
                        return;
                    continue;
                }

                if (Interlocked.CompareExchange(ref _mouseMoveCts, null, cts) != cts)
                    continue;

                cts.Dispose();

                if (_lastPointerMoveEvents is not null)
                {
                    _tooltipVisible = true;
                    TooltipService.StartTooltip(_tooltipKey, await GetTooltipInfo(_lastPointerMoveEvents, await BoundsService.GetWindowBoundsAsync().ConfigureAwait(false)));
                }
            }
        }
        finally
        {
            _mouseMoveDebounceRunning = false;
        }
    }
}
