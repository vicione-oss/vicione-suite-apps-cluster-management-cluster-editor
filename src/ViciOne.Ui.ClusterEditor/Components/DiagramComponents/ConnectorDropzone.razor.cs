using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.Blazor.Components.Draggable.Components;
using ViciOne.Ui.Blazor.Components.Draggable.Services;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components.DiagramComponents;

/// <summary>
/// Makes a single connector a drop target for the drag currently under way. The connector joins a drag only
/// when the drop policy admits it, so the target is known by which dropzone fired, not by where the pointer
/// landed.
/// </summary>
/// <remarks>
/// <see cref="ConnectorDropTargets"/> admits and marks the connectors of all dropzones together.
/// </remarks>
public sealed partial class ConnectorDropzone : ComponentBase, IDropzone, IDisposable
{
    private ElementReference _element;

    [Inject] private ConnectorDropTargets ConnectorDropTargets { get; set; } = default!;
    [Inject] private IDropHandler<BlockNodeConnector> DropHandler { get; set; } = default!;

    [Parameter, EditorRequired]
    public BlockNodeConnector Connector { get; set; } = default!;

    public void Dispose()
        => ConnectorDropTargets.Unregister(this);

    /// <inheritdoc/>
    public Task DragDroppedAsync(IDraggable draggable, double x, double y)
        => DropHandler.DragDroppedAsync(draggable, x, y, Connector);

    /// <inheritdoc/>
    public Task DragEndAsync(IDraggable draggable, double x, double y)
    {
        ConnectorDropTargets.ClearMarks();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DragEnterAsync(IDraggable draggable)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task DragLeaveAsync()
        => Task.CompletedTask;

    /// <inheritdoc/>
    public ElementReference GetElementReference()
        => _element;

    protected override void OnInitialized()
        => ConnectorDropTargets.Register(this);
}
