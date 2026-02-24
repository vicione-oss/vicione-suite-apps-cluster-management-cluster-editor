#if DEBUG

using System;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.ClusterEditor.Sections.Debugging.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.Debugging.Components;

public sealed partial class DebugConsole : ComponentBase, IDisposable
{
    [Inject] private DebugService DebugService { get; set; } = default!;

    public void Dispose()
    {
        DebugService.LogChanged -= OnChangedAsync;
        DebugService.ValueChanged -= OnChangedAsync;
    }

    private async void OnChangedAsync()
        => await InvokeAsync(StateHasChanged);

    protected override void OnInitialized()
    {
        DebugService.LogChanged += OnChangedAsync;
        DebugService.ValueChanged += OnChangedAsync;
    }

    private async void SetDbgHeightPxAsync(int pxHeight)
    {
        DebugService.VisualLogHeightPx = pxHeight;
        await InvokeAsync(StateHasChanged);
    }

    private async void ToggleDebugLogAsync()
    {
        DebugService.ShowMessageLog ^= true;
        await InvokeAsync(StateHasChanged);
    }
}

#endif
