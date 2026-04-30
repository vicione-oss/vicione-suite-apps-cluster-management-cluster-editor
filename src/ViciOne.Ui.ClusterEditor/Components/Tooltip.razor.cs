using System;
using System.Threading.Tasks;
using Blazor.Diagrams.Core.Geometry;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.Blazor.Components.Resizing.Models;
using ViciOne.Ui.Blazor.Components.Resizing.Services;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components;

public sealed partial class Tooltip : ComponentBase, IDisposable
{
    private const int TooltipPositioningMarginPx = 10;

    private TooltipInfo? _info;
    private bool _isVisible;
    private int _positionLeft = -1;
    private int _positionTop = -1;
    private ElementReference _refTooltipContainer;
    private Size _tooltipContainerSize = Size.Zero;

    [Inject] private IResizeObserver ResizeObserver { get; set; } = default!;
    [Inject] private TooltipService TooltipService { get; set; } = default!;

    private void CalculateTooltipPosition()
    {
        if (_info is null)
            return;

        _positionLeft = (_info.PosX + TooltipPositioningMarginPx + Convert.ToInt32(_tooltipContainerSize.Width)) > _info.ParentBounds.Right
            ? _info.PosX - Convert.ToInt32(_tooltipContainerSize.Width) - TooltipPositioningMarginPx
            : _info.PosX + TooltipPositioningMarginPx;

        _positionTop = (_info.PosY + TooltipPositioningMarginPx + Convert.ToInt32(_tooltipContainerSize.Height)) > _info.ParentBounds.Height
            ? _info.PosY - Convert.ToInt32(_tooltipContainerSize.Height) - TooltipPositioningMarginPx
            : _info.PosY + TooltipPositioningMarginPx;
    }

    public void Dispose()
    {
        TooltipService.HideTooltip -= OnTooltipServiceHideTooltip;
        TooltipService.ShowTooltip -= OnTooltipServiceShowTooltip;

        ResizeObserver.ElementSizeChanged -= OnResizeObserverChanged;
        _ = ResizeObserver.UnobserveAsync(_refTooltipContainer);

        GC.SuppressFinalize(this);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ResizeObserver.ObserveAsync(_refTooltipContainer);
            ResizeObserver.ElementSizeChanged += OnResizeObserverChanged;
        }
    }

    protected override void OnInitialized()
    {
        TooltipService.HideTooltip += OnTooltipServiceHideTooltip;
        TooltipService.ShowTooltip += OnTooltipServiceShowTooltip;
    }

    private void OnResizeObserverChanged(ElementSizeChangedEventArgs args)
    {
        if (args.ElementReference.Id == _refTooltipContainer.Id)
        {
            _tooltipContainerSize = new(args.DomRect.Width, args.DomRect.Height);
            CalculateTooltipPosition();
            _isVisible = true;
            InvokeAsync(StateHasChanged);
        }
    }

    private void OnTooltipServiceHideTooltip()
    {
        _info = null;
        _isVisible = false;
        _positionLeft = -1;
        _positionTop = -1;

        InvokeAsync(StateHasChanged);
    }

    private void OnTooltipServiceShowTooltip(TooltipInfo tooltipInfo)
    {
        _isVisible = false;
        _info = tooltipInfo;

        InvokeAsync(StateHasChanged);
    }
}
