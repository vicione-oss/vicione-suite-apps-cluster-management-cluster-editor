#if DEBUG

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.ClusterEditor.Sections.Debugging.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.Debugging.Components;

public sealed partial class DebugConsole : ComponentBase, IDisposable
{
    [Inject] private DebugService DebugService { get; set; } = default!;

    public void Dispose()
    {
        DebugService.LogChanged -= OnChanged;
        DebugService.ValueChanged -= OnChanged;
    }

    private async void OnChanged()
        => await InvokeAsync(StateHasChanged);

    protected override void OnInitialized()
    {
        DebugService.LogChanged += OnChanged;
        DebugService.ValueChanged += OnChanged;
    }

    private async Task SetDbgHeightPx(int pxHeight)
    {
        DebugService.VisualLogHeightPx = pxHeight;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ToggleDebugLog()
    {
        DebugService.ShowMessageLog ^= true;
        await InvokeAsync(StateHasChanged);
    }
}

#endif
