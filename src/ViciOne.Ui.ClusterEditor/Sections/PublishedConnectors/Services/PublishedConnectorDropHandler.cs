using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ViciOne.Ui.Blazor.Components.Draggable.Components;
using ViciOne.Ui.Blazor.Components.Draggable.Services;
using ViciOne.Ui.Blazor.Components.Tables.Shared.Models;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;

namespace ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;

/// <summary>
/// Links the dragged published connectors to the connector they were dropped on. The dropzone that fired is
/// the target, so the drop coordinates are ignored.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class PublishedConnectorDropHandler(PublishedConnectorsService publishedConnectorsService)
    : IDropHandler<BlockNodeConnector>
{
    /// <inheritdoc/>
    public Task DragDroppedAsync(IDraggable draggable, double x, double y, BlockNodeConnector target)
    {
        if (draggable is IDraggableRowSet<DataGridConnectorWrapper> rowSet)
            publishedConnectorsService.AddLinks(rowSet.Items, target);

        return Task.CompletedTask;
    }
}
