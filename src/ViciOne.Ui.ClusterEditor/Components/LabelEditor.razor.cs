using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ViciOne.Ui.Blazor.Components.Dialog.Components;
using ViciOne.Ui.ClusterEditor.Extensions;

namespace ViciOne.Ui.ClusterEditor.Components;

public sealed partial class LabelEditor : ComponentBase, IAsyncDisposable
{
    private bool _disposed;
    private string _editorVisibility = "visible";
    private IJSObjectReference? _jsModule;
    private Dialog? _refDialog;

    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] private ILogger<LabelEditor> Logger { get; set; } = default!;

    internal event Action<string>? LabelEditorClosed;

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        await DisposeModuleAsync();
    }

    private async ValueTask DisposeModuleAsync()
    {
        var module = _jsModule;
        _jsModule = null;

        if (module is null)
            return;

        await module.TryDisposeAsync(Logger);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        var (_, module) = await JsRuntime.TryInvoke<IJSObjectReference>(
            Logger,
            "import",
            "./_content/ViciOne.Ui.ClusterEditor/Components/LabelEditor.razor.js");

        _jsModule = module;

        if (_disposed)
            await DisposeModuleAsync();
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

        if (_jsModule is not null)
        {
            var (success, value) = await _jsModule.TryInvoke<string>(Logger, "getValue");

            if (success && value is not null)
                LabelEditorClosed?.Invoke(value);
        }

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

        if (_jsModule is not null)
            await _jsModule.TryInvokeVoid(Logger, "showEditor", content);
    }
}
