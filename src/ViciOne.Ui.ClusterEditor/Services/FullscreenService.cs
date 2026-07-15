using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ViciOne.Ui.ClusterEditor.Extensions;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed class FullscreenService(IJSRuntime jSRuntime, ILogger<FullscreenService> logger) : IDisposable
{
    private DotNetObjectReference<FullscreenService>? _refObject;

    public ElementReference DataflowEditorContainerReference { get; set; }
    public bool IsFullscreen { get; private set; }

    public event Func<bool, Task>? FullscreenStateChanged;

    public void Dispose()
        => _refObject?.Dispose();

    [JSInvokable("fullscreenExitHandler")]
    public async Task FullscreenExitHandler()
    {
        IsFullscreen = false;
        await FullscreenStateChanged.InvokeEventAsync(false, logger, nameof(FullscreenStateChanged));
    }

    public async Task SetFullscreen(bool isFullscreen)
    {
        IsFullscreen = isFullscreen;
        await FullscreenStateChanged.InvokeEventAsync(isFullscreen, logger, nameof(FullscreenStateChanged));

        _refObject ??= DotNetObjectReference.Create(this);

        await jSRuntime.InvokeVoidAsync(
            "ViciOne.Element.setFullscreen",
            isFullscreen,
            _refObject,
            "fullscreenExitHandler"
        );
    }

    public async Task ToggleFullscreen()
        => await SetFullscreen(!IsFullscreen);
}
