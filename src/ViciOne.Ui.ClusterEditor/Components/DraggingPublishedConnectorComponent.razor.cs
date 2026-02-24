using System;
using Blazor.Diagrams.Core.Geometry;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;

namespace ViciOne.Ui.ClusterEditor.Components;

public sealed partial class DraggingPublishedConnectorComponent : ComponentBase, IDisposable
{
    private bool _isValidTargetNearby;
    private Point? _position;

    [Inject] private PublishedConnectorsService PublishedConnectorsService { get; set; } = default!;

    public void Dispose()
        => PublishedConnectorsService.DraggingPublishedConnectorPositionChanged -= OnDraggingPublishedConnectorPositionChanged;

    private void OnDraggingPublishedConnectorPositionChanged(Point? position, bool isValidTargetNearby)
    {
        _isValidTargetNearby = isValidTargetNearby;
        _position = position;
        StateHasChanged();
    }

    protected override void OnInitialized()
        => PublishedConnectorsService.DraggingPublishedConnectorPositionChanged += OnDraggingPublishedConnectorPositionChanged;
}
