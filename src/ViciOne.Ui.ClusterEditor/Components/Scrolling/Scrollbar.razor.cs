using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.Blazor.Components.Resizing.Models;
using ViciOne.Ui.Blazor.Components.Resizing.Services;

namespace ViciOne.Ui.ClusterEditor.Components.Scrolling;

public sealed partial class Scrollbar : ComponentBase, IDisposable
{
    private int _containerLength;
    private int _externalScrolledPixels;
    private bool _isDisabled;
    private bool _isDisabledChanged;
    private Point? _lastClientPointerPos;
    private ElementReference _refScrollbarContainer;
    private int _scrollablePixels;
    private int _scrolledPixels;
    private double _scrollValue;
    private int _thumbLength;

    [Inject] private IResizeObserver ResizeObserver { get; set; } = default!;

    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public int MaxScrollablePixels { get; set; }
    [Parameter] public int MaxVisiblePixels { get; set; }
    [Parameter] public ScrollbarOrientation Orientation { get; set; }
    [Parameter] public int? ScrolledPixels { get; set; }
    [Parameter] public EventCallback<int> ScrolledPixelsChanged { get; set; }
    [Parameter] public double? ScrollValue { get; set; }
    [Parameter] public EventCallback<double> ScrollValueChanged { get; set; }
    [Parameter] public EventCallback<PointerEventArgs> ThumbPointerUp { get; set; }
    [Parameter] public double WheelScrollMultiplier { get; set; } = 0.25;
    [Parameter] public double ZeroScrollValue { get; set; }

    public event Action? ScrollingStarted;
    public event Action? ScrollingStopped;

    public void Dispose()
    {
        ResizeObserver.ElementSizeChanged -= OnElementSizeChangedAsync;
        _ = ResizeObserver.UnobserveAsync(_refScrollbarContainer);
    }

    private string GetStateCssClasses()
    {
        var resultBuilder = new StringBuilder();

        resultBuilder.Append(Orientation switch
        {
            ScrollbarOrientation.Vertical => "vertical",
            ScrollbarOrientation.Horizontal => "horizontal",
            _ => throw new ArgumentException(nameof(Orientation)),
        });
        resultBuilder.Append(' ');

        if (IsVisible)
        {
            resultBuilder.Append("is-visible");
            resultBuilder.Append(' ');
        }

        if (_isDisabled)
            resultBuilder.Append("disabled");

        return resultBuilder.ToString();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ResizeObserver.ObserveAsync(_refScrollbarContainer);
            ResizeObserver.ElementSizeChanged += OnElementSizeChangedAsync;
        }
    }

    private async Task OnContainerClickAsync(MouseEventArgs e)
    {
        if (_lastClientPointerPos != null)
            return;

        var scrollPositive = Orientation switch
        {
            ScrollbarOrientation.Horizontal => e.OffsetX > _scrolledPixels + (_thumbLength / 2),
            ScrollbarOrientation.Vertical => e.OffsetY > _scrolledPixels + (_thumbLength / 2),
            _ => throw new ArgumentException(nameof(Orientation)),
        };

        await RecalculateScrollbarPositionAsync(Convert.ToInt32(scrollPositive ? _thumbLength : -_thumbLength));
    }

    private async void OnElementSizeChangedAsync(ElementSizeChangedEventArgs args)
    {
        if (args.ElementReference.Id == _refScrollbarContainer.Id)
        {
            _containerLength = Orientation switch
            {
                ScrollbarOrientation.Vertical => (int)args.DomRect.Height,
                ScrollbarOrientation.Horizontal => (int)args.DomRect.Width,
                _ => throw new ArgumentException(nameof(Orientation)),
            };

            await RecalculateBaseValuesAsync();
        }
    }

    protected override async Task OnParametersSetAsync()
        => await RecalculateBaseValuesAsync();

    private void OnThumbPointerDown(MouseEventArgs e)
    {
        _lastClientPointerPos = new(e.ClientX, e.ClientY);
        ScrollingStarted?.Invoke();
    }

    private void OnThumbPointerLeave(PointerEventArgs _)
        => StopScolling();

    private async Task OnThumbPointerMoveAsync(MouseEventArgs e)
    {
        if (_lastClientPointerPos == null)
            return;

        var currentClientPointerPos = new Point(e.ClientX, e.ClientY);

        var pointerDrag = Orientation switch
        {
            ScrollbarOrientation.Vertical => currentClientPointerPos.Y - _lastClientPointerPos.Y,
            ScrollbarOrientation.Horizontal => currentClientPointerPos.X - _lastClientPointerPos.X,
            _ => throw new ArgumentException(nameof(Orientation)),
        };

        _lastClientPointerPos = currentClientPointerPos;
        await RecalculateScrollbarPositionAsync(Convert.ToInt32(pointerDrag));
    }

    private async Task OnThumbPointerUp(PointerEventArgs e)
    {
        if (ThumbPointerUp.HasDelegate)
            await ThumbPointerUp.InvokeAsync(e);

        StopScolling();
    }

    internal async Task OnWheelScrollAsync(WheelEventArgs e)
    {
        var wheelDeltaY = Orientation switch
        {
            ScrollbarOrientation.Vertical => e.DeltaY,
            ScrollbarOrientation.Horizontal => -e.DeltaY,
            _ => throw new ArgumentException(nameof(Orientation)),
        };

        await RecalculateScrollbarPositionAsync(Convert.ToInt32(wheelDeltaY * WheelScrollMultiplier));
    }

    private async Task RecalculateBaseValuesAsync()
    {
        var recalculationNeeded = false;

        if (MaxScrollablePixels != 0)
        {
            var newThumbLength = Convert.ToInt32(1.0 * MaxVisiblePixels / MaxScrollablePixels * _containerLength);
            if (newThumbLength != _thumbLength)
            {
                _thumbLength = newThumbLength;
                _scrollablePixels = Convert.ToInt32(_containerLength - _thumbLength);
                recalculationNeeded = true;
            }
        }

        if (ScrollValue.HasValue && ScrollValue.Value != _scrollValue)
        {
            _scrollValue = ScrollValue.Value;
            recalculationNeeded = true;
        }
        else if (ScrolledPixels.HasValue && ScrolledPixels.Value != _externalScrolledPixels)
        {
            if (ScrolledPixels > MaxScrollablePixels)
                throw new InvalidOperationException($"{nameof(ScrolledPixels)} cannot be larger then {MaxScrollablePixels}.");

            var externalScrollablePixels = MaxScrollablePixels - MaxVisiblePixels;
            if (externalScrollablePixels != 0)
            {
                _scrollValue = 1.0 * ScrolledPixels.Value / externalScrollablePixels;
                recalculationNeeded = true;
            }
        }

        var isNowDisabled = MaxScrollablePixels <= MaxVisiblePixels;
        if (isNowDisabled != _isDisabled)
        {
            _isDisabled = isNowDisabled;
            _isDisabledChanged = true;
        }

        if (recalculationNeeded)
            await RecalculateScrollbarPositionAsync(0);
    }

    private async Task RecalculateScrollbarPositionAsync(int delta)
    {
        if (!_isDisabled)
        {
            if (_scrollablePixels != 0)
                _scrollValue += 1.0 * delta / _scrollablePixels;

            _scrollValue = Math.Clamp(_scrollValue, 0, 1);
            _scrolledPixels = Convert.ToInt32(_scrollablePixels * _scrollValue);

            var externalScrollablePixels = MaxScrollablePixels - MaxVisiblePixels;
            _externalScrolledPixels = Convert.ToInt32((externalScrollablePixels * _scrollValue) - (externalScrollablePixels * ZeroScrollValue));
        }
        else if (_isDisabledChanged)
        {
            _isDisabledChanged = false;
            _scrollValue = ZeroScrollValue;
            _scrolledPixels = Convert.ToInt32(_scrollablePixels * ZeroScrollValue);
            _externalScrolledPixels = Convert.ToInt32((MaxScrollablePixels - MaxVisiblePixels) * ZeroScrollValue);
        }
        else
        {
            return;
        }

        await ScrolledPixelsChanged.InvokeAsync(_externalScrolledPixels);
        await ScrollValueChanged.InvokeAsync(_scrollValue);
        await InvokeAsync(StateHasChanged);
    }

    private void StopScolling()
    {
        ScrollingStopped?.Invoke();
        _lastClientPointerPos = null;
    }
}
