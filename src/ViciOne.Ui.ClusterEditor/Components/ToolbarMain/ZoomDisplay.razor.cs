using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components.ToolbarMain;

public sealed partial class ZoomDisplay : ComponentBase, IDisposable
{
    private const string FitString = "Fit";
    private const double UiMaximumZoom = DiagramSettings.ZoomMaximum;
    private const double UiMinimumZoom = 0.05;
    private const double UiZoomStep = 0.05;

    private string _currentZoomText = $"{Convert.ToInt32(DiagramSettings.DefaultZoom * 100)}%";
    private bool _selfUpdated;
    private bool _zoomingToFit;
    private readonly List<string> _zoomValues = ["10%", "25%", "50%", "75%", "100%", "125%", "150%", "200%", FitString];

    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;
    [Inject] private DiagramService DiagramService { get; set; } = default!;

    [Parameter] public bool Enabled { get; set; } = true;

    private string ZoomText
    {
        get => _currentZoomText;
        set
        {
            if (value == _currentZoomText)
                return;

            var parsedValue = value.Contains('%', StringComparison.Ordinal)
                ? value.Trim('%')
                : value;

            if (value == FitString)
            {
                _zoomingToFit = true;
                DiagramEventService.RequestZoomToFit();
            }
            else if (double.TryParse(parsedValue, out var inputValue))
            {
                var matchedZoomLevel = Math.Clamp(
                    inputValue / 100,
                    DiagramSettings.ZoomMinimum,
                    DiagramSettings.ZoomMaximum
                );

                _selfUpdated = true;
                _currentZoomText = $"{Convert.ToInt32(matchedZoomLevel * 100)}%";
                DiagramService.SetZoom(matchedZoomLevel);
            }
            else
            {
                _selfUpdated = true;
                _currentZoomText = value;
            }
        }
    }

    private void ChangeZoomValue(bool increment)
    {
        if (DiagramService.DiagramState.Zoom < UiMinimumZoom)
        {
            if (increment)
                DiagramService.SetZoom(UiMinimumZoom);
        }
        else if (DiagramService.DiagramState.Zoom > UiMaximumZoom)
        {
            if (!increment)
                DiagramService.SetZoom(UiMaximumZoom);

        }
        else
        {
            var newDiagramZoom = Math.Round(DiagramService.DiagramState.Zoom + (increment ? UiZoomStep : -UiZoomStep), 2);
            newDiagramZoom = Math.Clamp(newDiagramZoom, UiMinimumZoom, UiMaximumZoom);
            DiagramService.SetZoom(newDiagramZoom);
        }
    }

    public void Dispose()
    {
        DiagramEventService.ZoomChanged -= OnDiagramZoomChangedAsync;
        GC.SuppressFinalize(this);
    }

    private async void OnDiagramZoomChangedAsync(double updatedZoom)
    {
        if (_zoomingToFit)
            _zoomingToFit = false;

        if (!_selfUpdated || _zoomingToFit)
        {
            _currentZoomText = $"{Convert.ToInt32(updatedZoom * 100)}%";
            await InvokeAsync(StateHasChanged);
        }

        if (_selfUpdated)
            _selfUpdated = false;
    }

    protected override void OnInitialized()
    {
        DiagramEventService.ZoomChanged += OnDiagramZoomChangedAsync;
        OnDiagramZoomChangedAsync(DiagramService.DiagramState.Zoom);
    }
}
