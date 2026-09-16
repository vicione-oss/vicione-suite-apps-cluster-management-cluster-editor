using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ViciOne.Ui.ClusterEditor.Extensions;

namespace ViciOne.Ui.ClusterEditor.Components.NodeEditorServices;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class NodeEditorJsInterop(IJSRuntime jsRuntime, ILogger<NodeEditorJsInterop> logger)
{
    private const string DiagramCanvasClass = "diagram-canvas";
    private const string FocusByClassIdentifier = "ViciOne.Element.focusByClass";
    private const string WaitForNodesIdentifier = "ViciOne.NodeMove.waitForNodes";

    public Task FocusDiagramCanvasAsync()
        => jsRuntime.TryInvokeVoid(logger, FocusByClassIdentifier, DiagramCanvasClass);

    public async Task<bool> WaitForNodesAsync(IEnumerable<string> ids, CancellationToken ct)
    {
        var (success, result) = await jsRuntime.TryInvoke<bool>(logger, WaitForNodesIdentifier, ct, ids);
        return success && result;
    }
}
