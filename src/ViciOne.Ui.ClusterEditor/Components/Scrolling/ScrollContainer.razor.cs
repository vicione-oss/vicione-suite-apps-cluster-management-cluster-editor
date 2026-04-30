using System;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.Blazor.Components.Resizing.Models;
using ViciOne.Ui.Blazor.Components.Resizing.Services;

namespace ViciOne.Ui.ClusterEditor.Components.Scrolling;

public sealed partial class ScrollContainer : ComponentBase, IDisposable
{
    private Size _allocationContainerSize = Size.Zero;
    private Size _contentContainerSize = Size.Zero;
    private bool _isScrollbarVisible;
    private ElementReference _refAllocationContainer;
    private ElementReference _refContentContainer;
    private Scrollbar? _refOneWayScrollbar;
    private readonly Timer _scrollbarVisibilityTimer = new()
    {
        AutoReset = false,
        Enabled = false,
        Interval = 2000,
    };
    private int _scrolledPixels;
    private bool _scrollToEnd;

    [Inject] private IResizeObserver ResizeObserver { get; set; } = default!;

    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string? CssClass { get; set; }

    public int ScrolledPixels => _scrolledPixels;

    public event Action<Size>? AllocationContainerSizeChanged;
    public event Action<int>? ScrolledPixelsChanged;

    public void Dispose()
    {
        _scrollbarVisibilityTimer.Elapsed -= OnScrollbarVisibilityTimerElapsedAsync;
        _scrollbarVisibilityTimer.Dispose();

        ResizeObserver.ElementSizeChanged -= OnElementSizeChangedAsync;
        _ = ResizeObserver.UnobserveAsync(_refAllocationContainer);
        _ = ResizeObserver.UnobserveAsync(_refContentContainer);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ResizeObserver.ObserveAsync(_refAllocationContainer);
            await ResizeObserver.ObserveAsync(_refContentContainer);
        }
    }

    private void OnContainerPointerEnter()
    {
        _scrollbarVisibilityTimer.Stop();
        _isScrollbarVisible = true;
    }

    private void OnContainerPointerLeave()
        => _scrollbarVisibilityTimer.Start();

    private async void OnElementSizeChangedAsync(ElementSizeChangedEventArgs args)
    {
        var update = false;

        var newSize = args.DomRect;

        if (args.ElementReference.Id == _refAllocationContainer.Id)
        {
            _allocationContainerSize = new(newSize.Width, newSize.Height);
            AllocationContainerSizeChanged?.Invoke(_allocationContainerSize);
            update = true;
        }
        else if (args.ElementReference.Id == _refContentContainer.Id)
        {
            if (newSize.Height < _contentContainerSize.Height && newSize.Height < (_scrolledPixels + _allocationContainerSize.Height))
                SetScrolledPixels(Convert.ToInt32(Math.Max(0, newSize.Height - _allocationContainerSize.Height)));

            _contentContainerSize = new(newSize.Width, newSize.Height);
            update = true;
        }

        if (update)
        {
            if (_scrollToEnd)
            {
                ScrollToEnd();
                _scrollToEnd = false;
            }

            await InvokeAsync(StateHasChanged);
        }
    }

    protected override void OnInitialized()
    {
        _scrollbarVisibilityTimer.Elapsed += OnScrollbarVisibilityTimerElapsedAsync;
        ResizeObserver.ElementSizeChanged += OnElementSizeChangedAsync;
    }

    private async void OnScrollbarVisibilityTimerElapsedAsync(object? _1, ElapsedEventArgs _2)
    {
        _isScrollbarVisible = false;
        _scrollbarVisibilityTimer.Stop();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnScrolledPixelsChangedAsync(int newScrolledPixels)
    {
        SetScrolledPixels(newScrolledPixels);
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnWheelScrollAsync(WheelEventArgs e)
        => await _refOneWayScrollbar!.OnWheelScrollAsync(e);

    private void ScrollToEnd()
    {
        if (_contentContainerSize.Height <= _allocationContainerSize.Height)
            return;

        SetScrolledPixels(Convert.ToInt32(_contentContainerSize.Height - _allocationContainerSize.Height));
    }

    public void SetAutoscrollToEnd()
        => _scrollToEnd = true;

    private void SetScrolledPixels(int scrolledPixels)
    {
        _scrolledPixels = scrolledPixels;
        ScrolledPixelsChanged?.Invoke(scrolledPixels);
    }
}
