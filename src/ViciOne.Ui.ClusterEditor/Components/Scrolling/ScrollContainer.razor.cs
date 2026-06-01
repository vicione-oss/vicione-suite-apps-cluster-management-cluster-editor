using System;
using System.Threading;
using System.Threading.Tasks;
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
    private bool _scrollbarHideDebounceRunning;
    private CancellationTokenSource? _scrollbarVisibilityCts;
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
        var oldCts = Interlocked.Exchange(ref _scrollbarVisibilityCts, null);
        oldCts?.Cancel();
        oldCts?.Dispose();

        ResizeObserver.ElementSizeChanged -= OnElementSizeChanged;
        _ = ResizeObserver.UnobserveAsync(_refAllocationContainer);
        _ = ResizeObserver.UnobserveAsync(_refContentContainer);
    }

    private async Task HideScrollbarAfterDelay()
    {
        try
        {
            while (true)
            {
                var cts = _scrollbarVisibilityCts;
                if (cts is null)
                    return;

                try
                {
                    await Task.Delay(2000, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    if (_scrollbarVisibilityCts is null)
                        return;
                    continue;
                }

                if (Interlocked.CompareExchange(ref _scrollbarVisibilityCts, null, cts) == cts)
                {
                    cts.Dispose();
                    _isScrollbarVisible = false;
                    await InvokeAsync(StateHasChanged).ConfigureAwait(false);
                    return;
                }
            }
        }
        finally
        {
            _scrollbarHideDebounceRunning = false;
        }
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
        var oldCts = Interlocked.Exchange(ref _scrollbarVisibilityCts, null);
        oldCts?.Cancel();
        oldCts?.Dispose();
        _isScrollbarVisible = true;
    }

    private void OnContainerPointerLeave()
        => StartScrollbarHideDebounce();

    private async void OnElementSizeChanged(ElementSizeChangedEventArgs args)
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
        => ResizeObserver.ElementSizeChanged += OnElementSizeChanged;

    private async Task OnScrolledPixelsChanged(int newScrolledPixels)
    {
        SetScrolledPixels(newScrolledPixels);
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnWheelScroll(WheelEventArgs e)
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

    private void StartScrollbarHideDebounce()
    {
        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _scrollbarVisibilityCts, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();

        if (_scrollbarHideDebounceRunning)
            return;

        _scrollbarHideDebounceRunning = true;
        _ = HideScrollbarAfterDelay();
    }
}
