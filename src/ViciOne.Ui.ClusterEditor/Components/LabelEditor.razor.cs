using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.Blazor.Components.Dialog.Components;

namespace ViciOne.Ui.ClusterEditor.Components;

public sealed partial class LabelEditor : ComponentBase, IAsyncDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private string _editorVisibility = "visible";
    private IJSObjectReference? _jsModule;
    private Dialog? _refDialog;

    [Inject] private IContextMenuSettings ContextMenuSettings { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    internal event Action<string>? LabelEditorClosed;

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();

        try
        {
            if (_jsModule is not null)
                await _jsModule.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>(
                    "import",
                    _cts.Token,
                    "./_content/ViciOne.Ui.ClusterEditor/Components/LabelEditor.razor.js");
            }
            catch (OperationCanceledException) { }
            catch (JSDisconnectedException) { }
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

        await _refDialog.ShowAsync();

        _editorVisibility = "visible";
        await InvokeAsync(StateHasChanged);
        await _jsModule!.InvokeVoidAsync("showEditor", content);
    }
}
