using System;
using Blazor.Diagrams.Core.Geometry;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.TreeEditor.Builder.Interface.Icons;

namespace ViciOne.Ui.ClusterEditor.Components;

public sealed partial class DraggingDataPoint : ComponentBase, IDisposable
{
    private IIcon? _icon;
    private Point? _position;
    private bool _visible;

    [Inject] private IDataPortTreeIconProvider DataPortTreeIconProvider { get; set; } = default!;
    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private DragService DragService { get; set; } = default!;

    public void Dispose()
    {
        DragService.DraggingEnded -= OnDraggingEnded;
        DragService.DraggingPositionChanged -= OnDraggingConnectorPositionChanged;
        DragService.DraggingStarted -= OnDraggingStarted;
    }

    private async void OnDraggingConnectorPositionChanged(Point? position)
    {
        _position = position;
        await InvokeAsync(StateHasChanged);
    }

    private async void OnDraggingEnded(Point _)
    {
        _visible = false;
        _position = null;
        await InvokeAsync(StateHasChanged);
    }

    private void OnDraggingStarted()
    {
        if (DragService.DraggedItems is null)
            return;

        foreach (var item in DragService.DraggedItems)
        {
            if (item is not DataPortChildNodeModel dataPortChildNodeModel)
                continue;

            _icon = DataPortTreeIconProvider.GetDataPointIcon(dataPortChildNodeModel, 32, Datastore.Builder.Cache);
            _visible = true;
            return;
        }
    }

    protected override void OnInitialized()
    {
        DragService.DraggingEnded += OnDraggingEnded;
        DragService.DraggingPositionChanged += OnDraggingConnectorPositionChanged;
        DragService.DraggingStarted += OnDraggingStarted;
    }
}
