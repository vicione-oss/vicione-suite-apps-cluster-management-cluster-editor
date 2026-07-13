using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.Blazor.Components.ContextMenu.Components;
using ViciOne.Ui.ClusterEditor.Models.ComponentStates.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Models.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components.ContextMenu.Specialized;

public sealed partial class BlockNodeConnectorContextMenu : SpecializedContextMenuWithStateBase<BlockNodeConnectorContextMenuContext, BlockNodeConnectorContextMenuState>, IAsyncDisposable
{
    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;
    [Inject] private DiagramService DiagramService { get; set; } = default!;
    [Inject] private SelectionManager SelectionManager { get; set; } = default!;

    private void AddConnectorToParentContainerClick()
    {
        var connectorEditor = Datastore.Builder.Editors.Connector;
        var isChildContainer = Datastore.ActiveContainer is ChildContainer;
        var activeContainer = isChildContainer ? (ChildContainer)Datastore.ActiveContainer : null;

        var selectedConModels = Datastore.DataflowDiagramMapping.GetModels(SelectionManager.SelectedConnectors);
        var addableConnectors = selectedConModels
            .Where(c => !IsOnParentContainer(c))
            .Where(c => connectorEditor.CanAddToParentContainer(c))
            .ToArray();

        if (addableConnectors.Length == 0)
            return;

        HashSet<BlockNode> effectedNodes = [];
        foreach (var connectorModel in addableConnectors)
        {
            var connector = Datastore.DataflowDiagramMapping.GetDiagramModel(connectorModel);
            var parentContainerConnectorModel = connectorEditor.AddToParentContainer(connectorModel).First();

            connector.SetIsOnContainer(true);
            connector.SetParentContainerConnector(parentContainerConnectorModel);

            effectedNodes.Add(connector.Node);
        }

        foreach (var node in effectedNodes)
            node.Refresh();

        bool IsOnParentContainer(IConnector connector)
            => isChildContainer && activeContainer!.GetConnector(connector.GetUnderlyingConnector()) is not null;
    }

    private void CancelConnectorPublicationClick()
        => SetPublishedForSelectedConnectors(false);

    private async Task CloseContextMenuRequested()
    {
        if (ContextMenu is not null)
            await ContextMenu.CloseAsync();
    }

    public async ValueTask DisposeAsync()
        => DiagramEventService.CloseContextMenuRequested -= CloseContextMenuRequested;

    private void OnContextMenuVisibilityChanged(bool isVisible)
    {
        DiagramService.DiagramState.IsContextMenuVisible = isVisible;

        if (isVisible)
        {
            DiagramEventService.CloseContextMenuRequested += CloseContextMenuRequested;
        }
        else
        {
            DiagramEventService.CloseContextMenuRequested -= CloseContextMenuRequested;
            DiagramEventService.RequestDiagramFocus();
        }
    }

    private void PublishConnectorClick()
        => SetPublishedForSelectedConnectors(true);

    private async Task RemoveConnectorFromContainerClickAsync()
    {
        var connectorEditor = Datastore.Builder.Editors.Connector;
        var selectedChildContainerConnectors = SelectionManager.SelectedConnectors.Where(c => c.Node is ChildContainerNode).ToArray();

        var selectedConnectors = Datastore.DataflowDiagramMapping.GetModels(selectedChildContainerConnectors);
        var containerConnectors = selectedConnectors.Select(c =>
        {
            var underlyingConnector = c.GetUnderlyingConnector();
            var upstreamConnectors = underlyingConnector.GetAllUpstreamContainerConnectors().ToArray();
            var result = upstreamConnectors.FirstOrDefault(c =>
                Datastore.ActiveContainer.Containers.Contains(c.Container.Parent));

            return result ?? (IConnector)underlyingConnector;
        }).ToArray();

        connectorEditor.RemoveFromParentContainer(containerConnectors);

        foreach (var connector in selectedChildContainerConnectors)
            SelectionManager.Deselect(connector);

        var nodes = selectedChildContainerConnectors.GroupBy(c => (ChildContainerNode)c.Node);
        foreach (var grp in nodes)
        {
            grp.Key.RemoveConnectors(grp, Datastore);
        }
    }

    private void RemoveConnectorFromParentContainerClick()
    {
        var connectorEditor = Datastore.Builder.Editors.Connector;
        var selectedConnectors = Datastore.GetVisibleConnectorModels(SelectionManager.SelectedConnectors);
        connectorEditor.RemoveFromParentContainer([.. selectedConnectors]);

        foreach (var connector in SelectionManager.SelectedConnectors)
            connector.SetIsOnContainer(false);

        var nodes = SelectionManager.SelectedConnectors.Select(c => c.Node).Distinct();
        foreach (var node in nodes)
            node.Refresh();
    }

    private void SetPublishedForSelectedConnectors(bool published)
    {
        var connectorEditor = Datastore.Builder.Editors.Connector;
        var updatedConModels = Datastore.DataflowDiagramMapping.GetModels(SelectionManager.SelectedConnectors)
            .Where(cm => cm.Published != published)
            .ToArray();

        foreach (var connector in updatedConModels)
            connectorEditor.SetPublished(connector.GetUnderlyingConnector(), published);
    }
}
