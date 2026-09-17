using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.TreeEditor.Builder;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class DataPortDragCoordinator(DragService dragService, IDatastore datastore)
{
    public void Attach(ITreeBuilder builder)
    {
        builder.DragAndDrop.EnableInbound = true;
        builder.DragAndDrop.EnableOutbound = true;
        builder.DragAndDrop.DisplayElementShadow = false;

        builder.DragAndDrop.DragEnded += OnDragEnded;
        builder.DragAndDrop.Dragged += OnDragged;
        builder.DragAndDrop.DragStarted += OnDragStarted;
    }

    public void Detach(ITreeBuilder builder)
    {
        builder.DragAndDrop.DragEnded -= OnDragEnded;
        builder.DragAndDrop.Dragged -= OnDragged;
        builder.DragAndDrop.DragStarted -= OnDragStarted;
    }

    private void OnDragEnded()
        => dragService.EndDragging();

    private void OnDragged(DragEventArgs e)
        => dragService.OnPointerMove(e);

    private void OnDragStarted(IEnumerable<ITreeNode> treeNodes)
    {
        // Nodes below a node in edit mode must not be linked, see DataPortChildNodeModelExtensions.IsLockedByEditMode.
        // The list is materialized here because the dragged items are enumerated again on drop.
        List<IDragable> draggedItems = [.. treeNodes
            .OfType<IDragable>()
            .Where(n => n is not DataPortChildNodeModel childNode || !childNode.IsLockedByEditMode())];

        List<BlockNodeConnector> possibleTargetConnectors = [];

        foreach (var dataPortChildNode in draggedItems.OfType<DataPortChildNodeModel>())
        {
            var dataPortTreeNode = datastore.Builder.Cache.DataPortTreeNodeIds.GetValueOrDefault(dataPortChildNode.Id.Value);
            if (dataPortTreeNode is null)
                continue;

            possibleTargetConnectors.AddRange(dataPortTreeNode.GetValidTargetConnectors(datastore, dataPortChildNode));
        }

        // Always start dragging, even without items, as DragService.EndDragging does not reset the items and targets of a previous drag.
        dragService.StartDragging(draggedItems, possibleTargetConnectors, false);
    }
}
