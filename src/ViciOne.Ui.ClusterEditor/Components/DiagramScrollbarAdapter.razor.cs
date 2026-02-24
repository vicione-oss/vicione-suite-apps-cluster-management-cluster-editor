using System;
using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.Shared.Dx.Components.Scrolling;

namespace ViciOne.Ui.ClusterEditor.Components;

public sealed partial class DiagramScrollbarAdapter : ComponentBase, IDisposable
{
    private Rectangle _diagramNodeBounds = Rectangle.Zero;
    private Rectangle _diagramViewport = Rectangle.Zero;
    private int _maxScrollablePixels;
    private int _maxVisiblePixels;
    private double _scrollValue;
    private bool _shouldRender;

    [CascadingParameter] internal Diagram? Diagram { get; set; }

    [Parameter] public ScrollbarOrientation Orientation { get; set; }

    public void Dispose()
    {
        Diagram!.Changed -= OnDiagramChangedAsync;
        Diagram.ContainerChanged -= OnDiagramChangedAsync;
        Diagram.Nodes.Added -= OnDiagramNodeAdded;
        Diagram.Nodes.Removed -= OnDiagramNodeRemoved;

        foreach (var node in Diagram.Nodes)
            node.Changed -= OnDiagramNodeChanged;

        GC.SuppressFinalize(this);
    }

    private bool IsContained()
        => Orientation switch
        {
            ScrollbarOrientation.Horizontal => (_diagramNodeBounds.Right >= _diagramViewport.Left)
                                            && (_diagramNodeBounds.Left >= _diagramViewport.Left - _diagramNodeBounds.Width)
                                            && (_diagramNodeBounds.Left <= _diagramViewport.Right)
                                            && (_diagramNodeBounds.Right <= _diagramViewport.Right + _diagramNodeBounds.Width),
            ScrollbarOrientation.Vertical => (_diagramNodeBounds.Bottom >= _diagramViewport.Top)
                                          && (_diagramNodeBounds.Top >= _diagramViewport.Top - _diagramNodeBounds.Height)
                                          && (_diagramNodeBounds.Top <= _diagramViewport.Bottom)
                                          && (_diagramNodeBounds.Bottom <= _diagramViewport.Bottom + _diagramNodeBounds.Height),
            _ => throw new ArgumentException($"Unknown value of {nameof(ScrollbarOrientation)}."),
        };

    private async void OnDiagramChangedAsync()
    {
        _diagramNodeBounds = Diagram!.GetNodeBounds();
        _diagramViewport = Diagram!.GetViewport();

        var isFinite = Orientation switch
        {
            ScrollbarOrientation.Horizontal
                => double.IsFinite(_diagramViewport.Width)
                && double.IsFinite(_diagramViewport.Left)
                && double.IsFinite(_diagramNodeBounds.Width)
                && double.IsFinite(_diagramNodeBounds.Left),
            ScrollbarOrientation.Vertical
                => double.IsFinite(_diagramViewport.Height)
                && double.IsFinite(_diagramViewport.Top)
                && double.IsFinite(_diagramNodeBounds.Height)
                && double.IsFinite(_diagramNodeBounds.Top),
            _ => throw new ArgumentException($"Unknown value of {nameof(ScrollbarOrientation)}."),
        };

        if (!isFinite)
            return;

        _maxVisiblePixels = Orientation switch
        {
            ScrollbarOrientation.Horizontal => (int)_diagramViewport.Width,
            ScrollbarOrientation.Vertical => (int)_diagramViewport.Height,
            _ => throw new ArgumentException($"Unknown value of {nameof(ScrollbarOrientation)}."),
        };

        var (nvOffsetX, nvOffsetY) = Diagram!.GetNodeViewportDistance();
        if (IsContained())
        {
            _maxScrollablePixels = Orientation switch
            {
                ScrollbarOrientation.Horizontal => Convert.ToInt32(_diagramViewport.Width + _diagramNodeBounds.Width),
                ScrollbarOrientation.Vertical => Convert.ToInt32(_diagramViewport.Height + _diagramNodeBounds.Height),
                _ => throw new ArgumentException($"Unknown value of {nameof(ScrollbarOrientation)}."),
            };

            _scrollValue = Orientation switch
            {
                ScrollbarOrientation.Horizontal => (nvOffsetX / (_maxScrollablePixels / 2.0) / 2) + 0.5,
                ScrollbarOrientation.Vertical => (nvOffsetY / (_maxScrollablePixels / 2.0) / 2) + 0.5,
                _ => throw new ArgumentException($"Unknown value of {nameof(ScrollbarOrientation)}."),
            };
        }
        else
        {
            var unionRect = new Rectangle(
                Math.Min(_diagramViewport.Left, _diagramNodeBounds.Left),
                Math.Min(_diagramViewport.Top, _diagramNodeBounds.Top),
                Math.Max(_diagramViewport.Left + _diagramViewport.Width, _diagramNodeBounds.Left + _diagramNodeBounds.Width),
                Math.Max(_diagramViewport.Top + _diagramViewport.Height, _diagramNodeBounds.Top + _diagramNodeBounds.Height)
            );

            _maxScrollablePixels = Orientation switch
            {
                ScrollbarOrientation.Horizontal => Convert.ToInt32(unionRect.Width),
                ScrollbarOrientation.Vertical => Convert.ToInt32(unionRect.Height),
                _ => throw new ArgumentException($"Unknown value of {nameof(ScrollbarOrientation)}."),
            };

            _scrollValue = Orientation switch
            {
                ScrollbarOrientation.Horizontal => nvOffsetX > 0 ? 1 : 0,
                ScrollbarOrientation.Vertical => nvOffsetY > 0 ? 1 : 0,
                _ => throw new ArgumentException($"Unknown value of {nameof(ScrollbarOrientation)}."),
            };
        }

        _shouldRender = true;
        await InvokeAsync(StateHasChanged);
    }

    private void OnDiagramNodeAdded(NodeModel node)
        => node.Changed += OnDiagramNodeChanged;

    private void OnDiagramNodeChanged(Model _) => OnDiagramChangedAsync();

    private void OnDiagramNodeRemoved(NodeModel node)
        => node.Changed -= OnDiagramNodeChanged;

    protected override void OnInitialized()
    {
        ArgumentNullException.ThrowIfNull(Diagram, nameof(Diagram));

        Diagram.Changed += OnDiagramChangedAsync;
        Diagram.ContainerChanged += OnDiagramChangedAsync;
        Diagram.Nodes.Added += OnDiagramNodeAdded;
        Diagram.Nodes.Removed += OnDiagramNodeRemoved;
    }

    private void OnScrollValueChanged(double newScrollValue)
    {
        if (newScrollValue == _scrollValue)
            return;

        var (nvOffsetX, nvOffsetY) = Diagram!.GetNodeViewportDistance();
        var panUpdateValue = (_scrollValue - newScrollValue) * _maxScrollablePixels * Diagram!.Zoom;

        switch (Orientation)
        {
            case ScrollbarOrientation.Horizontal:
                _scrollValue = IsContained() ? newScrollValue : (nvOffsetX > 0 ? 1 : 0);
                Diagram!.UpdatePan(panUpdateValue, 0);
                break;
            case ScrollbarOrientation.Vertical:
                _scrollValue = IsContained() ? newScrollValue : (nvOffsetY > 0 ? 1 : 0);
                Diagram!.UpdatePan(0, panUpdateValue);
                break;
            default:
                throw new ArgumentException($"Unknown value of {nameof(ScrollbarOrientation)}.");
        }
    }

    protected override bool ShouldRender()
    {
        if (_shouldRender)
        {
            _shouldRender = false;
            return true;
        }

        return _shouldRender;
    }
}
