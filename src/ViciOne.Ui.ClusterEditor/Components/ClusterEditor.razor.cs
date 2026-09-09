using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Models;
using ViciOne.Ui.ClusterEditor.Components.ToolbarDataflow.Models;
using ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.Shared.Dx.Services;

namespace ViciOne.Ui.ClusterEditor.Components;

public sealed partial class ClusterEditor : ComponentBase, IDisposable
{
    private DotNetObjectReference<InputEventService>? _refObject;

    [Inject] private ComparerService ComparerService { get; set; } = default!;
    [Inject] private IPropertyGridState<ContainerEditorPropertyGridContext> ContainerEditorPropertyGridState { get; set; } = default!;
    [Inject] private IPropertyGridState<DataflowToolbarPropertyGridContext> DataflowToolbarPropertyGridState { get; set; } = default!;
    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;
    [Inject] private FullscreenService FullscreenService { get; set; } = default!;
    [Inject] private InputEventService InputEventService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    public void Dispose()
    {
        _refObject?.Dispose();
        ConnectorMapper.Dispose(DiagramEventService);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _refObject = DotNetObjectReference.Create(InputEventService);
            await JSRuntime.InvokeVoidAsync("ViciOne.InputEvents.initializeKeyboardListener", _refObject);
        }
    }

    protected override void OnInitialized()
    {
        ConnectorMapper.Init(DiagramEventService);
        ComparerService.InitCache();

        DataflowToolbarPropertyGridState.GroupByCategory = true;
        DataflowToolbarPropertyGridState.KeepMessages = true;

        ContainerEditorPropertyGridState.GroupByCategory = true;
        ContainerEditorPropertyGridState.KeepMessages = true;
    }
}
