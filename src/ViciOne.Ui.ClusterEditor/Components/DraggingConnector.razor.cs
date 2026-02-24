using System;
using System.Linq;
using Blazor.Diagrams.Core.Geometry;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Components;

public sealed partial class DraggingConnector : ComponentBase, IDisposable
{
    private string _connectorColor = BlockNodeConnectorColors.TypeDefault;
    private bool _isInput;
    private Point? _position;
    private bool _visible;

    [Inject] private DragService DragService { get; set; } = default!;

    public void Dispose()
    {
        DragService.DraggingEnded -= OnDraggingEnded;
        DragService.DraggingPositionChanged -= OnDraggingConnectorPositionChanged;
        DragService.DraggingStarted -= OnDraggingStarted;
    }

    private void OnDraggingConnectorPositionChanged(Point? position)
    {
        _position = position;
        StateHasChanged();
    }

    private void OnDraggingEnded(Point _)
    {
        _visible = false;
        _position = null;
        StateHasChanged();
    }

    private void OnDraggingStarted()
    {
        if (DragService.DraggedItems?.FirstOrDefault(di => di is BlockNodeConnector) is not BlockNodeConnector connectorNode)
            return;

        _visible = true;
        _isInput = connectorNode.IsInput;
        _connectorColor = connectorNode.PortColor ?? BlockNodeConnectorColors.TypeDefault;
    }

    protected override void OnInitialized()
    {
        DragService.DraggingEnded += OnDraggingEnded;
        DragService.DraggingPositionChanged += OnDraggingConnectorPositionChanged;
        DragService.DraggingStarted += OnDraggingStarted;
    }
}
