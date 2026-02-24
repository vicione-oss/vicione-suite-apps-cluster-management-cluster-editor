using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed class FullscreenService(IJSRuntime jSRuntime) : IDisposable
{
    private DotNetObjectReference<FullscreenService>? _refObject;

    public ElementReference DataflowEditorContainerReference { get; set; }
    public bool IsFullscreen { get; private set; }

    public event Action<bool>? FullscreenStateChanged;

    public void Dispose()
        => _refObject?.Dispose();

    [JSInvokable("fullscreenExitHandler")]
    public void FullscreenExitHandler()
    {
        IsFullscreen = false;
        FullscreenStateChanged?.Invoke(false);
    }

    public async Task SetFullscreenAsync(bool isFullscreen)
    {
        IsFullscreen = isFullscreen;
        FullscreenStateChanged?.Invoke(isFullscreen);

        _refObject ??= DotNetObjectReference.Create(this);

        await jSRuntime.InvokeVoidAsync(
            "ViciOne.Element.setFullscreen",
            isFullscreen,
            _refObject,
            "fullscreenExitHandler"
        );
    }

    public async Task ToggleFullscreenAsync()
        => await SetFullscreenAsync(!IsFullscreen);
}
