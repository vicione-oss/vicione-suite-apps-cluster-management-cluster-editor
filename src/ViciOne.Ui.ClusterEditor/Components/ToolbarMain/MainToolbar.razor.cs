using System;
using System.Threading.Tasks;
using Blazor.Diagrams.Core.Models.Base;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components.ToolbarMain;

public sealed partial class MainToolbar : ComponentBase, IDisposable
{
    private bool _alignButtonsEnabledState;
    private bool _isFullscreen;

    [Inject] private DiagramService DiagramService { get; set; } = default!;
    [Inject] private FullscreenService FullscreenService { get; set; } = default!;
    [Inject] private SelectionManager SelectionManager { get; set; } = default!;

    public void Dispose()
    {
        FullscreenService.FullscreenStateChanged -= OnFullscreenStateChanged;
        SelectionManager.DiagramSelectionChanged -= OnSelectionChanged;

        GC.SuppressFinalize(this);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
            SelectionManager.DiagramSelectionChanged += OnSelectionChanged;
    }

    private async Task OnFullscreenStateChanged(bool isFullscreen)
    {
        _isFullscreen = isFullscreen;
        await InvokeAsync(StateHasChanged);
    }

    protected override void OnInitialized()
        => FullscreenService.FullscreenStateChanged += OnFullscreenStateChanged;

    private void OnNodeAlignRequested(Alignment align)
        => align.ApplyToSelection(SelectionManager);

    private async Task OnSelectionChanged(SelectableModel _)
    {
        _alignButtonsEnabledState = SelectionManager.SelectedBlockNodes.Count + SelectionManager.SelectedLabels.Count >= 2 && DiagramService.DiagramState.IsInitialized;
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnToggleFullscreenClick()
        => await FullscreenService.ToggleFullscreen();
}
