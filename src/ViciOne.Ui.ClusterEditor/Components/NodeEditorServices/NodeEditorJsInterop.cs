using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace ViciOne.Ui.ClusterEditor.Components.NodeEditorServices;

internal sealed class NodeEditorJsInterop(IJSRuntime jsRuntime)
{
    private const string DiagramCanvasClass = "diagram-canvas";
    private const string FocusByClassIdentifier = "ViciOne.Element.focusByClass";
    private const string WaitForNodesIdentifier = "ViciOne.NodeMove.waitForNodes";

    public Task FocusDiagramCanvasAsync()
        => jsRuntime.InvokeVoidAsync(FocusByClassIdentifier, DiagramCanvasClass).AsTask();

    public Task<bool> WaitForNodesAsync(IEnumerable<string> ids, CancellationToken ct)
        => jsRuntime.InvokeAsync<bool>(WaitForNodesIdentifier, ct, ids).AsTask();
}
