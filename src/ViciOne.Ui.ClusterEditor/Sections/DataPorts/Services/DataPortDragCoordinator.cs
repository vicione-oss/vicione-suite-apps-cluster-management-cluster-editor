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
        List<BlockNodeConnector> possibleTargetConnectors = [];

        foreach (var treeNode in treeNodes)
        {
            if (treeNode is not DataPortChildNodeModel dataPortChildNode)
                continue;

            var dataPortTreeNode = datastore.Builder.Cache.DataPortTreeNodeIds.GetValueOrDefault(dataPortChildNode.Id.Value);
            if (dataPortTreeNode is null)
                continue;

            possibleTargetConnectors.AddRange(dataPortTreeNode.GetValidTargetConnectors(datastore, dataPortChildNode));
        }

        dragService.StartDragging(treeNodes.OfType<IDragable>(), possibleTargetConnectors, false);
    }
}
