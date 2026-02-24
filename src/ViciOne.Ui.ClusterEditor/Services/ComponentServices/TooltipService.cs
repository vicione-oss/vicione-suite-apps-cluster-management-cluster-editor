using System;
using System.Collections.Generic;
using System.Timers;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Services.ComponentServices;

public sealed class TooltipService : IDisposable
{
    private readonly DiagramEventService _diagramEventService;
    private bool _hasActiveTooltip;
    private readonly Timer _renderDelayTimer = new()
    {
        AutoReset = false,
        Enabled = false,
        Interval = 1150,
    };
    private readonly Stack<TooltipInfo> _tooltipInfos = [];

    public event Action? HideTooltip;
    public event Action<TooltipInfo>? ShowTooltip;

    public TooltipService(DiagramEventService diagramEventService)
    {
        _diagramEventService = diagramEventService;
        _diagramEventService.ContainerLoaded += OnDiagramContainerLoaded;
        _diagramEventService.ContainerRemoved += OnDiagramContainerRemoved;
        _diagramEventService.FunctionBlockRemoved += Reset;

        _renderDelayTimer.Elapsed += OnRenderDelayTimerElapsed;
    }

    public void Dispose()
    {
        _diagramEventService.ContainerLoaded -= OnDiagramContainerLoaded;
        _diagramEventService.ContainerRemoved -= OnDiagramContainerRemoved;
        _diagramEventService.FunctionBlockRemoved -= Reset;

        _renderDelayTimer.Elapsed -= OnRenderDelayTimerElapsed;
        _renderDelayTimer.Dispose();

        GC.SuppressFinalize(this);
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

    private void OnRenderDelayTimerElapsed(object? sender, ElapsedEventArgs e)
        => EmitShowTooltip();

    private void Reset()
    {
        HideTooltip?.Invoke();
        _hasActiveTooltip = false;
        _tooltipInfos.Clear();
        _renderDelayTimer.Stop();
    }

    private void RestartRenderDelayTimer()
    {
        _renderDelayTimer.Stop();
        _renderDelayTimer.Start();
    }

    public void StartTooltip(TooltipInfo tooltipInfo)
    {
        _tooltipInfos.Push(tooltipInfo);

        if (_hasActiveTooltip)
            EmitShowTooltip();
        else
            RestartRenderDelayTimer();
    }

    public void StopTooltip()
    {
        _renderDelayTimer.Stop();

        _tooltipInfos.TryPop(out var _);

        if (_tooltipInfos.Count > 0)
        {
            if (_hasActiveTooltip)
                EmitShowTooltip();
            else
                _renderDelayTimer.Start();
        }
        else
        {
            _hasActiveTooltip = false;
            HideTooltip?.Invoke();
        }
    }
}
