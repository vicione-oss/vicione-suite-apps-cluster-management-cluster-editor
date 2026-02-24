using System;
using System.Linq;
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
        FullscreenService.FullscreenStateChanged -= OnFullscreenStateChangedAsync;
        SelectionManager.DiagramSelectionChanged -= OnSelectionChangedAsync;

        GC.SuppressFinalize(this);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
            SelectionManager.DiagramSelectionChanged += OnSelectionChangedAsync;
    }

    private async void OnFullscreenStateChangedAsync(bool isFullscreen)
    {
        _isFullscreen = isFullscreen;
        await InvokeAsync(StateHasChanged);
    }

    protected override void OnInitialized()
        => FullscreenService.FullscreenStateChanged += OnFullscreenStateChangedAsync;

    private void OnNodeAlignRequested(Alignment align)
        => align.ApplyToSelection(SelectionManager);

    private async void OnSelectionChangedAsync(SelectableModel _)
    {
        _alignButtonsEnabledState = SelectionManager.SelectedBlockNodes.Count() + SelectionManager.SelectedLabels.Count() >= 2 && DiagramService.DiagramState.IsInitialized;
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnToggleFullscreenClickAsync()
        => await FullscreenService.ToggleFullscreenAsync();
}
