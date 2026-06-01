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
    private readonly Stack<TooltipInfo> _tooltipInfos = [];

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

        var tooltipInfo = _tooltipInfos.Peek();
        ShowTooltip?.Invoke(tooltipInfo);

        _hasActiveTooltip = true;
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

    public void StartTooltip(TooltipInfo tooltipInfo)
    {
        _tooltipInfos.Push(tooltipInfo);

        if (_hasActiveTooltip)
            EmitShowTooltip();
        else
            StartRenderDelay();
    }

    public void StopTooltip()
    {
        CancelRenderDelay();

        _tooltipInfos.TryPop(out var _);

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
