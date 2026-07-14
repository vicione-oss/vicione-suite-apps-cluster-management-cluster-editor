using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Services.ComponentServices;

public sealed class TooltipService : IDisposable
{
    private readonly DiagramEventService _diagramEventService;
    private bool _disposed;
    private bool _hasActiveTooltip;
    private CancellationTokenSource? _renderDelayCts;

    // Ordered list of active tooltips keyed by their owner (the UI element that
    // requested them). The last entry is the one currently shown on top.
    // Using an owner key instead of a blind stack ensures that leaving one
    // element removes *its* tooltip, regardless of the order enter/leave events
    // arrive in (which is not strictly LIFO once connectors/markers appear on
    // hover in Simplified View).
    private readonly List<(object Owner, TooltipInfo Info)> _tooltipInfos = [];

    public event Action? HideTooltip;
    public event Action<TooltipInfo>? ShowTooltip;

    public TooltipService(DiagramEventService diagramEventService)
    {
        _diagramEventService = diagramEventService;
        _diagramEventService.ContainerLoaded += OnDiagramContainerLoaded;
        _diagramEventService.ContainerRemoved += OnDiagramContainerRemoved;
        _diagramEventService.FunctionBlockRemoved += Reset;
    }

    private void CancelRenderDelay()
    {
        var oldCts = Interlocked.Exchange(ref _renderDelayCts, null);
        oldCts?.Cancel();
        oldCts?.Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _diagramEventService.ContainerLoaded -= OnDiagramContainerLoaded;
        _diagramEventService.ContainerRemoved -= OnDiagramContainerRemoved;
        _diagramEventService.FunctionBlockRemoved -= Reset;

        CancelRenderDelay();
    }

    private void EmitShowTooltip()
    {
        if (_tooltipInfos.Count < 1)
            return;

        var tooltipInfo = _tooltipInfos[^1].Info;
        ShowTooltip?.Invoke(tooltipInfo);

        _hasActiveTooltip = true;
    }

    private int IndexOfOwner(object owner)
    {
        for (var i = 0; i < _tooltipInfos.Count; i++)
        {
            if (_tooltipInfos[i].Owner == owner)
                return i;
        }

        return -1;
    }

    private void OnDiagramContainerLoaded(Container _)
        => Reset();

    private void OnDiagramContainerRemoved(Container _)
        => Reset();

    private void Reset()
    {
        HideTooltip?.Invoke();
        _hasActiveTooltip = false;
        _tooltipInfos.Clear();
        CancelRenderDelay();
    }

    private async Task ShowTooltipDebounced(CancellationToken cancellationToken)
    {
        await Task.Delay(1150, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        if (cancellationToken.IsCancellationRequested || _disposed)
            return;

        EmitShowTooltip();
    }

    private void StartRenderDelay()
    {
        if (_disposed)
            return;

        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _renderDelayCts, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();
        _ = ShowTooltipDebounced(newCts.Token);
    }

    public void StartTooltip(object owner, TooltipInfo tooltipInfo)
    {
        ArgumentNullException.ThrowIfNull(owner);

        // Replace any existing tooltip for this owner and move it to the top.
        var existingIndex = IndexOfOwner(owner);
        if (existingIndex >= 0)
            _tooltipInfos.RemoveAt(existingIndex);

        _tooltipInfos.Add((owner, tooltipInfo));

        if (_hasActiveTooltip)
            EmitShowTooltip();
        else
            StartRenderDelay();
    }

    public void StopTooltip(object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        CancelRenderDelay();

        var index = IndexOfOwner(owner);
        if (index >= 0)
            _tooltipInfos.RemoveAt(index);

        if (_tooltipInfos.Count > 0)
        {
            if (_hasActiveTooltip)
                EmitShowTooltip();
            else
                StartRenderDelay();
        }
        else
        {
            _hasActiveTooltip = false;
            HideTooltip?.Invoke();
        }
    }
}
