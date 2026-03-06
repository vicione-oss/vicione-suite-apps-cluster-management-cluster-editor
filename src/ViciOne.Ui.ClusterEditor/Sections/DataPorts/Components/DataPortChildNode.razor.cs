using System;
using System.Timers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.ClusterEditor.Helpers;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Templates;
using ViciOne.Ui.TreeEditor.Templates.Fragments.Node;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Components;

public sealed partial class DataPortChildNode : NodeTemplate, IAsyncDisposable
{
    private ActionButtonParameters? _actionButtonContainerParameters;
    private DropAreaParameters? _dropAreaParameters;
    private PointerEventArgs? _lastPointerMoveEvents;
    private readonly Timer _mouseMoveTimer = new();
    private bool _tooltipVisible;

    [Inject] private BoundsService BoundsService { get; set; } = default!;
    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private TooltipService TooltipService { get; set; } = default!;

    protected override void Calculate()
    {
        // reset internal drop zone state on refresh if drop got disabled
        if (_dropAreaParameters is not null && !Node.DropZoneActive)
            _dropAreaParameters.CurrentlyOverDropZone = DropZone.None;

        _actionButtonContainerParameters?.CalculateCss();
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

        _mouseMoveTimer.Elapsed -= OnMouseMoveTimerElapsedAsync;
        _mouseMoveTimer.Dispose();
    }

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

        _mouseMoveTimer.AutoReset = false;
        _mouseMoveTimer.Interval = 200;
        _mouseMoveTimer.Elapsed += OnMouseMoveTimerElapsedAsync;
    }

    private async void OnMouseMoveTimerElapsedAsync(object? _, ElapsedEventArgs _1)
    {
        if (_lastPointerMoveEvents is not null)
        {
            _tooltipVisible = true;
            TooltipService.StartTooltip(TooltipDataPortData.GetDataPortTooltipInfo(Datastore, _lastPointerMoveEvents, (DataPortNodeModel)Node.TreeNode, await BoundsService.GetWindowBoundsAsync()));
        }
    }

    private void OnNodeTextPointerLeave()
    {
        _lastPointerMoveEvents = null;
        _tooltipVisible = false;
        _mouseMoveTimer.Stop();
        TooltipService.StopTooltip();
    }

    private void OnNodeTextPointerMove(PointerEventArgs e)
    {
        _lastPointerMoveEvents = e;
        _mouseMoveTimer.Stop();

        if (!_tooltipVisible)
            _mouseMoveTimer.Start();
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
}
