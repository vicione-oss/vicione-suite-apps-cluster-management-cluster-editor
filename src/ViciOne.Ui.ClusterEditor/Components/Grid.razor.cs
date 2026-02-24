using System;
using System.Globalization;
using Blazor.Diagrams.Core;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Components;

public sealed partial class Grid : ComponentBase, IDisposable
{
    private double _dimensions;
    private string _gridDimensions = "10px";
    private string _gridPositionX = "0px";
    private string _gridPositionY = "0px";

    [CascadingParameter] internal Diagram? Diagram { get; set; }

    [Parameter] public GridMode Mode { get; set; }
    [Parameter] public double Size { get; set; }
    [Parameter] public double VisibleUntil { get; set; } = 0.5;

    private bool IsVisible
        => Diagram!.Zoom > VisibleUntil;

    public void Dispose()
    {
        Diagram!.PanChanged -= OnDiagramPanChangedAsync;
        Diagram.ZoomChanged -= OnDiagramZoomChangedAsync;

        GC.SuppressFinalize(this);
    }

    private async void OnDiagramPanChangedAsync()
    {
        UpdateGridPosition();
        await InvokeAsync(StateHasChanged);
    }

    private async void OnDiagramZoomChangedAsync()
    {
        UpdateGridDimensions();
        await InvokeAsync(StateHasChanged);
    }

    protected override void OnInitialized()
    {
        ArgumentNullException.ThrowIfNull(Diagram, nameof(Diagram));

        Diagram.PanChanged += OnDiagramPanChangedAsync;
        Diagram.ZoomChanged += OnDiagramZoomChangedAsync;

        UpdateGridDimensions();
    }

    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Diagram, nameof(Diagram));

    private void UpdateGridDimensions()
    {
        _dimensions = Size * Diagram!.Zoom;
        _gridDimensions = string.Format(CultureInfo.InvariantCulture, "{0}px", _dimensions);

        UpdateGridPosition();
    }

    private void UpdateGridPosition()
    {
        var gridPosX = Diagram!.Pan.X % _dimensions;
        var gridPosY = Diagram.Pan.Y % _dimensions;
        _gridPositionX = string.Format(CultureInfo.InvariantCulture, "{0}px", gridPosX);
        _gridPositionY = string.Format(CultureInfo.InvariantCulture, "{0}px", gridPosY);
    }
}
