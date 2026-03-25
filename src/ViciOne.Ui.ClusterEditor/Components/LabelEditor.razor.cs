using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.Shared.Dx.Components;

namespace ViciOne.Ui.ClusterEditor.Components;

public sealed partial class LabelEditor : ComponentBase, IAsyncDisposable
{
    private string _editorVisibility = "visible";
    private IJSObjectReference? _jsModule;
    private DxDialog? _refDialog;

    [Inject] private IContextMenuSettings ContextMenuSettings { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    internal event Action<string>? LabelEditorClosed;

    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null)
        {
            try
            {
                await _jsModule.DisposeAsync();
                _jsModule = null;
            }
            catch (JSDisconnectedException)
            {
                // JSDisconnectedException is trapped during module disposal
                // in case Blazor's SignalR circuit is lost.
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/ViciOne.Ui.ClusterEditor/Components/LabelEditor.razor.js");
        }
    }

    private async Task OnDialogCancel()
    {
        if (_refDialog is null)
            return;

        _editorVisibility = "hidden";

        await _refDialog.CloseAsync();
    }

    private async Task OnDialogOk()
    {
        if (_refDialog is null)
            return;

        var value = await _jsModule!.InvokeAsync<string>("getValue");

        LabelEditorClosed?.Invoke(value);

        _editorVisibility = "hidden";
        await _refDialog.CloseAsync();
    }

    public async Task Show(string content)
    {
        if (_refDialog is null)
            return;

        await _refDialog.OpenAsync();

        _editorVisibility = "visible";
        await InvokeAsync(StateHasChanged);
        await _jsModule!.InvokeVoidAsync("showEditor", content);
    }
}
