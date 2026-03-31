using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components;

public sealed partial class Minimap : ComponentBase, IDisposable
{
    private const string BorderStyleDefault = "solid";
    private const string BorderStyleTransparent = "dashed";

    private Size _containerSize = Size.Zero;
    private bool _fullRefreshNeeded;
    private bool _isVisible;
    private bool _movingViewport;
    private Point _movingViewportLastPoint = Point.Zero;
    private readonly List<MinimapNode> _nodes = [];
    private Rectangle _referenceRect = Rectangle.Zero;
    private double _referenceScale = 0.8;
    private int _refreshCounter;
    private bool _refreshNeeded;
    private PeriodicTimer? _renderTimer;
    private bool _shouldRender;
    private bool _transparentMode;
    private Rectangle _viewportRect = Rectangle.Zero;

    [CascadingParameter] internal Diagram? Diagram { get; set; }

    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;
    [Inject] private DiagramService DiagramService { get; set; } = default!;

    [Parameter] public double WidthPercentage { get; set; } = 0.25;

    private double CalculateRescaleFactor()
        => Diagram!.Zoom * (Diagram.GetViewport().Width / (_viewportRect.Width / _referenceScale));

    private MinimapNode CreateMinimapNode(NodeModel node)
    {
        var minimapNode = new MinimapNode(node.Id)
        {
            Type = GetMinimapNodeType(node),
            ZIndex = GetZIndex(node),
        };
        SetColors(node, minimapNode);
        return minimapNode;
    }

    public void Dispose()
    {
        _renderTimer?.Dispose();
        DiagramEventService.MinimapVisibilityChangeRequested -= OnMinimapVisibilityChangeRequested;
        UnsubscribeEvents();
    }

    private (bool AbutOnHorizontal, bool AbutOnVertical) DoesViewportAbutOn()
    {
        var (viewportOffsetX, viewportOffsetY) = GetViewportOffset();
        var viewportPositionWidth = viewportOffsetX + (_viewportRect.Width / _referenceScale);
        var viewportPositionHeight = viewportOffsetY + (_viewportRect.Height / _referenceScale);

        return (
            viewportOffsetX == 0 || viewportPositionWidth == _containerSize.Width,
            viewportOffsetY == 0 || viewportPositionHeight == _containerSize.Height
        );
    }

    private static MinimapNodeType GetMinimapNodeType(NodeModel nodeModel)
    {
        if (nodeModel is BlockNode)
            return MinimapNodeType.Block;
        else if (nodeModel is LabelNode)
            return MinimapNodeType.Label;
        else
            throw new ArgumentException("Unknown node type", nameof(nodeModel));
    }

    private (double OffsetX, double OffsetY) GetRectangleOffset(Rectangle rect)
    {
        double horizontalPercentage = 0;
        if (_referenceRect.Width != 0)
            horizontalPercentage = Math.Abs(rect.Left - _referenceRect.Left) / _referenceRect.Width;

        double verticalPercentage = 0;
        if (_referenceRect.Height != 0)
            verticalPercentage = Math.Abs(rect.Top - _referenceRect.Top) / _referenceRect.Height;

        return new(
            _containerSize.Width * horizontalPercentage,
            _containerSize.Height * verticalPercentage
        );
    }

    private Rectangle GetScaledRect(Rectangle rect)
        => new(
            ScaleValue(rect.Left), ScaleValue(rect.Top),
            ScaleValue(rect.Right), ScaleValue(rect.Bottom)
        );

    private (double OffsetX, double OffsetY) GetViewportOffset()
    {
        double horizontalPercentage = 0;
        if (_referenceRect.Width != 0)
            horizontalPercentage = Math.Abs(_viewportRect.Left - _referenceRect.Left) / _referenceRect.Width;

        double verticalPercentage = 0;
        if (_referenceRect.Height != 0)
            verticalPercentage = Math.Abs(_viewportRect.Top - _referenceRect.Top) / _referenceRect.Height;

        return new(
            _containerSize.Width * horizontalPercentage,
            _containerSize.Height * verticalPercentage
        );
    }

    private int GetZIndex(NodeModel nodeModel)
        => nodeModel.GetType().Name switch
        {
            // TODO: Sobald BlockNodes einen Z-Index haben hier nachziehen.
            //nameof(BlockNode) => ((BlockNode)nodeModel).ZIndex,
            nameof(LabelNode) => ((LabelNode)nodeModel).Order,
            _ => default,
        };

    private void InitializeNodeCollection()
    {
        foreach (var node in Diagram!.Nodes)
        {
            if (node is IDiagramModel dmNode && !dmNode.Visible)
                continue;

            var newNode = CreateMinimapNode(node);
            _nodes.Add(newNode);
            node.Changed += OnNodeChanged;
            node.Moving += OnNodeMoving;
        }
    }

    private void InitializeRenderTimer()
        => _renderTimer = new(TimeSpan.FromMilliseconds(40));

    private void OnContainerPointerEnter()
        => DiagramEventService.InvokeDiagramPointerLeave();

    private void OnEdgeDraggingVisibilityChangeRequested(bool visible)
    {
        _transparentMode = visible;
        RefreshInternal();
    }

    protected override void OnInitialized()
    {
        ArgumentNullException.ThrowIfNull(Diagram, nameof(Diagram));
        DiagramEventService.MinimapVisibilityChangeRequested += OnMinimapVisibilityChangeRequested;
        SetVisibility(DiagramService.DiagramState.IsMinimapVisible);
    }

    private void OnMinimapClick(MouseEventArgs e)
    {
        var (viewportOffsetX, viewportOffsetY) = GetViewportOffset();
        var rescaleFactor = CalculateRescaleFactor();

        Diagram!.UpdatePan(
            (viewportOffsetX - e.OffsetX + (_viewportRect.Width / _referenceScale / 2)) * rescaleFactor,
            (viewportOffsetY - e.OffsetY + (_viewportRect.Height / _referenceScale / 2)) * rescaleFactor
        );
    }

    private void OnMinimapColoringChanged()
    {
        foreach (var minimapNode in _nodes)
        {
            var nodeModel = Diagram?.Nodes.FirstOrDefault(nm => nm.Id == minimapNode.Id);
            if (nodeModel is null)
                return;

            SetColors(nodeModel, minimapNode);
            SetCssStyles(minimapNode);
        }

        RefreshInternal();
    }

    private void OnMinimapVisibilityChangeRequested(bool isVisible)
        => SetVisibility(isVisible);

    private void OnMinimapWheel(WheelEventArgs e)
    {
        // Berechnung übernommen & angepasst aus GimpZoomBehavior.OnWheel()
        if (Diagram!.Container is null || e.DeltaY == 0)
            return;

        if (!Diagram.Options.Zoom.Enabled)
            return;

        var scaleFactor = ((Diagram.Options.Zoom.ScaleFactor - 1) * (Math.Abs(e.DeltaY) / 100)) + 1;
        var scale = Math.Clamp(scaleFactor, 1.01, 2);
        var oldZoom = Diagram.Zoom;
        var deltaY = Diagram.Options.Zoom.Inverse ? e.DeltaY * -1 : e.DeltaY;
        var newZoom = deltaY > 0 ? oldZoom * scale : oldZoom / scale;
        newZoom = Math.Clamp(newZoom, Diagram.Options.Zoom.Minimum, Diagram.Options.Zoom.Maximum);

        if (newZoom < 0 || newZoom == Diagram.Zoom)
            return;

        var clientWidth = Diagram.Container!.Width;
        var clientHeight = Diagram.Container.Height;
        var widthDiff = (clientWidth * newZoom) - (clientWidth * oldZoom);
        var heightDiff = (clientHeight * newZoom) - (clientHeight * oldZoom);

        var rescaleFactor = CalculateRescaleFactor();
        var clientX = _viewportRect.Width / _referenceScale / 2 * rescaleFactor;
        var clientY = _viewportRect.Height / _referenceScale / 2 * rescaleFactor;

        var xFactor = (clientX - Diagram.Pan.X) / oldZoom / clientWidth;
        var yFactor = (clientY - Diagram.Pan.Y) / oldZoom / clientHeight;
        var newPanX = Diagram.Pan.X - (widthDiff * xFactor);
        var newPanY = Diagram.Pan.Y - (heightDiff * yFactor);

        Diagram.Batch(() =>
        {
            Diagram.SetPan(newPanX, newPanY);
            Diagram.SetZoom(newZoom);
        });

        RecalculateReferences();

        foreach (var minimapNode in _nodes)
        {
            var nodeModel = Diagram?.Nodes.FirstOrDefault(nm => nm.Id == minimapNode.Id);
            if (nodeModel is null)
                return;

            SetBounds(nodeModel, minimapNode);
            SetCssStyles(minimapNode);
        }

        RefreshInternal();
    }

    private void OnNodeAdded(NodeModel node)
    {
        var minimapNode = CreateMinimapNode(node);
        _nodes.Add(minimapNode);
        node.Changed += OnNodeChanged;
        node.Moving += OnNodeMoving;
    }

    private void OnNodeChanged(Model model)
    {
        if (model is not NodeModel nodeModel)
            return;

        var minimapNode = _nodes.Find(n => n.Id == model.Id);

        // Die Sichtbarkeit prüfen, da diese sich geändert haben kann z.B. beim Filtern
        if (model is IDiagramModel diagramModel)
        {
            if (diagramModel.Visible)
            {
                if (minimapNode is null)
                {
                    minimapNode = CreateMinimapNode(nodeModel);
                    _nodes.Add(minimapNode);
                }
            }
            else
            {
                if (minimapNode is not null)
                {
                    _nodes.Remove(minimapNode);
                    minimapNode = null;
                    RefreshInternal();
                }
            }
        }

        if (minimapNode is null)
            return;

        SetColors(nodeModel, minimapNode);
        SetCssStyles(minimapNode);
        RefreshInternal();
    }

    private void OnNodeMoving(MovableModel model)
    {
        var minimapNode = _nodes.Find(n => n.Id == model.Id);

        if (model is not NodeModel nodeModel || minimapNode is null)
            return;

        RecalculateReferences();
        SetBounds(nodeModel, minimapNode);
        SetCssStyles(minimapNode);
        RefreshInternal();
    }

    private void OnNodeRemoved(NodeModel node)
    {
        node.Changed -= OnNodeChanged;
        node.Moving -= OnNodeMoving;

        var minimapNode = _nodes.Find(n => n.Id == node.Id);
        if (minimapNode is not null)
            _nodes.Remove(minimapNode);

        RefreshInternal();
    }

    private void OnViewportGhostPointerDown(PointerEventArgs e)
    {
        _movingViewport = true;
        _movingViewportLastPoint = new Point(e.ClientX, e.ClientY);
        RefreshInternal();
    }

    private void OnViewportGhostPointerMove(PointerEventArgs e)
    {
        if (!_movingViewport)
            return;

        var rescaleFactor = CalculateRescaleFactor();
        var (abutOnHorizontal, abutOnVertical) = DoesViewportAbutOn();

        var deltaX = _movingViewportLastPoint.X - e.ClientX;
        if (abutOnHorizontal && Math.Abs(Diagram!.Pan.X) > 25000)
            deltaX = Math.CopySign(100, deltaX);
        else
            deltaX *= rescaleFactor * (abutOnHorizontal ? _referenceScale : 1);

        var deltaY = _movingViewportLastPoint.Y - e.ClientY;
        if (abutOnVertical && Math.Abs(Diagram!.Pan.Y) > 25000)
            deltaY = Math.CopySign(100, deltaY);
        else
            deltaY *= rescaleFactor * (abutOnVertical ? _referenceScale : 1);

        Diagram!.UpdatePan(deltaX, deltaY);
        _movingViewportLastPoint = new(e.ClientX, e.ClientY);
    }

    private void OnViewportGhostPointerUp()
    {
        _movingViewport = false;
        _movingViewportLastPoint = Point.Zero;
        RefreshInternal();
    }

    private void RecalculateNodeBounds()
    {
        RecalculateReferences();
        foreach (var minimapNode in _nodes)
        {
            var nodeModel = Diagram?.Nodes.FirstOrDefault(nm => nm.Id == minimapNode.Id);
            if (nodeModel is null)
                return;

            SetBounds(nodeModel, minimapNode);
            SetCssStyles(minimapNode);
        }
        RefreshInternal();
    }

    private void RecalculateReferences()
    {
        if (Diagram!.Container is null)
            return;

        var scaledWidth = Diagram.Container.Width * WidthPercentage;
        _containerSize = new(scaledWidth, scaledWidth / Diagram.Container.Width * Diagram.Container.Height);

        _viewportRect = GetScaledRect(Diagram.GetViewport());

        var nodeBounds = GetScaledRect(Diagram!.GetNodeBounds());
        var unionRect = new Rectangle(
            Math.Min(_viewportRect.Left, nodeBounds.Left),
            Math.Min(_viewportRect.Top, nodeBounds.Top),
            Math.Max(_viewportRect.Left + _viewportRect.Width, nodeBounds.Left + nodeBounds.Width),
            Math.Max(_viewportRect.Top + _viewportRect.Height, nodeBounds.Top + nodeBounds.Height)
        );

        var scaleFactor = _containerSize.Height / _containerSize.Width;
        if (unionRect.Height / unionRect.Width <= scaleFactor)
        {
            _referenceRect = new(
                new Point(unionRect.Left, unionRect.Top),
                new Size(unionRect.Width, unionRect.Width * scaleFactor)
            );
        }
        else
        {
            _referenceRect = new(
                new Point(unionRect.Left, unionRect.Top),
                new Size(unionRect.Height / scaleFactor, unionRect.Height)
            );
        }

        _referenceScale = _referenceRect.Width / _containerSize.Width;
    }

    internal void Refresh()
    {
        _fullRefreshNeeded = true;
        _refreshCounter = 0;
        _refreshNeeded = true;
    }

    private void RefreshInternal()
    {
        _refreshCounter++;
        _refreshNeeded = true;
    }

    private async void RunRenderTimerAsync()
    {
        if (_renderTimer is null)
            return;

        while (await _renderTimer.WaitForNextTickAsync())
        {
            if (_refreshCounter == 0 && _fullRefreshNeeded)
            {
                _fullRefreshNeeded = false;
                RecalculateNodeBounds();
            }

            if (_refreshNeeded)
            {
                if (_refreshCounter > 1 && !_fullRefreshNeeded)
                    _fullRefreshNeeded = true;

                _refreshCounter = 0;
                _shouldRender = true;
                _refreshNeeded = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private double ScaleValue(double value)
    {
        if (Diagram!.Container is null)
            return value * Diagram.Zoom;

        return value * Diagram.Zoom * (Diagram.Container.Width * WidthPercentage / Diagram.Container.Width);
    }

    private void SetBounds(NodeModel nodeModel, MinimapNode minimapNode)
    {
        var nodeRect = new Rectangle(nodeModel.Position, nodeModel.Size ?? Size.Zero);

        if (nodeRect == Rectangle.Zero)
            return;

        var scaledNodeRect = GetScaledRect(nodeRect);
        var (nodeOffsetX, nodeOffsetY) = GetRectangleOffset(scaledNodeRect);

        minimapNode.Bounds = new(new(nodeOffsetX, nodeOffsetY), new(scaledNodeRect.Width / _referenceScale, scaledNodeRect.Height / _referenceScale));
    }

    private void SetColors(NodeModel nodeModel, MinimapNode minimapNode)
    {
        if (nodeModel is BlockNode blockNode)
        {
            if (DiagramService.DiagramState.UseNodeColoringOnMinimap)
                minimapNode.BackgroundColor = blockNode.NameBackgroundColor;
            else
                minimapNode.BackgroundColor = MinimapColors.NodeBackgroundDefault;
        }
        else if (nodeModel is LabelNode labelNode)
        {
            if (DiagramService.DiagramState.UseNodeColoringOnMinimap)
            {
                minimapNode.BackgroundColor = labelNode.BackgroundColor;
                minimapNode.BorderColor = labelNode.BorderColor;
            }
            else
            {
                minimapNode.BackgroundColor = MinimapColors.LabelBackgroundDefault;
            }
        }

        if ((minimapNode.BackgroundColor == "rgba(0, 0, 0, 0)" || minimapNode.BackgroundColor == "transparent")
            && (minimapNode.BorderColor == "rgba(0, 0, 0, 0)" || minimapNode.BorderColor == "transparent"))
        {
            minimapNode.BorderColor = MinimapColors.BorderDefault;
            minimapNode.BorderStyle = BorderStyleTransparent;
        }
    }

    private static void SetCssStyles(MinimapNode node)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder
            .Append(CultureInfo.InvariantCulture, $"--node-background-color: {node.BackgroundColor};")
            .Append(CultureInfo.InvariantCulture, $"--node-border-color: {node.BorderColor};")
            .Append(CultureInfo.InvariantCulture, $"--node-border-style: {node.BorderStyle};")
            .Append(CultureInfo.InvariantCulture, $"--node-height: {node.Bounds.Height}px;")
            .Append(CultureInfo.InvariantCulture, $"--node-pos-x: {node.Bounds.Left}px;")
            .Append(CultureInfo.InvariantCulture, $"--node-pos-y: {node.Bounds.Top}px;")
            .Append(CultureInfo.InvariantCulture, $"--node-width: {node.Bounds.Width}px;")
            .Append(CultureInfo.InvariantCulture, $"--node-z-index: {(node.ZIndex == 0 ? "auto" : node.ZIndex)};");

        node.CssStyles = stringBuilder.ToString();
    }

    private async void SetVisibility(bool isVisible)
    {
        if (_isVisible == isVisible)
            return;

        _isVisible = isVisible;

        if (_isVisible)
        {
            InitializeNodeCollection();
            SubscribeEvents();
            RefreshInternal();
            InitializeRenderTimer();
            RunRenderTimerAsync(); // Fire-and-forget
        }
        else
        {
            UnsubscribeEvents();
            _nodes.Clear();
            _renderTimer?.Dispose();
            _renderTimer = null;
            _shouldRender = true;
            await InvokeAsync(StateHasChanged);
        }
    }

    protected override bool ShouldRender()
    {
        if (_shouldRender)
        {
            _shouldRender = false;
            return true;
        }

        return false;
    }

    private void SubscribeEvents()
    {
        Diagram!.ContainerChanged += RecalculateNodeBounds;
        Diagram!.PanChanged += RecalculateNodeBounds;
        Diagram!.ZoomChanged += RecalculateNodeBounds;

        Diagram!.Nodes.Added += OnNodeAdded;
        Diagram!.Nodes.Removed += OnNodeRemoved;

        DiagramEventService.EdgeDraggingVisibilityChangeRequested += OnEdgeDraggingVisibilityChangeRequested;
        DiagramEventService.MinimapColoringChanged += OnMinimapColoringChanged;
    }

    private void UnsubscribeEvents()
    {
        foreach (var node in Diagram!.Nodes)
        {
            node.Changed -= OnNodeChanged;
            node.Moving -= OnNodeMoving;
        }

        Diagram!.ContainerChanged -= RecalculateNodeBounds;
        Diagram!.PanChanged -= RecalculateNodeBounds;
        Diagram!.ZoomChanged -= RecalculateNodeBounds;

        Diagram!.Nodes.Added -= OnNodeAdded;
        Diagram!.Nodes.Removed -= OnNodeRemoved;

        DiagramEventService.EdgeDraggingVisibilityChangeRequested -= OnEdgeDraggingVisibilityChangeRequested;
        DiagramEventService.MinimapColoringChanged -= OnMinimapColoringChanged;
    }

    private record MinimapNode(string Id)
    {
        public string BackgroundColor { get; set; } = MinimapColors.LabelBackgroundDefault;
        public string BorderColor { get; set; } = MinimapColors.BorderDefault;
        public string BorderStyle { get; set; } = BorderStyleDefault;
        public Rectangle Bounds { get; set; } = Rectangle.Zero;
        public string CssStyles { get; set; } = string.Empty;
        public MinimapNodeType Type { get; set; }
        public int ZIndex { get; set; }
    }

    private enum MinimapNodeType
    {
        Block,
        Label,
    }
}
