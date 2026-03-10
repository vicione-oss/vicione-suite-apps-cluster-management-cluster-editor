using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Models.Base;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor;
using ViciOne.Ui.ClusterEditor.Components.ToolbarDataflow.Models;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Helpers;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components.DiagramComponents;

[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling", Justification = "TODO")]
public sealed partial class BlockComponent : ComponentBase, IDisposable, IHandleEvent
{
    private Block _block = new();
    private bool _isHovered;
    private BlockNode? _node;

    [CascadingParameter] internal Diagram? Diagram { get; set; }

    [Inject] private IContextMenuRequest<BlockNodeConnectorContextMenuContext> BlockNodeConnectorContextMenuRequest { get; set; } = default!;
    [Inject] private BoundsService BoundsService { get; set; } = default!;
    [Inject] private ConnectorService ConnectorService { get; set; } = default!;
    [Inject] private IContextMenuSettings ContextMenuSettings { get; set; } = default!;
    [Inject] private DataPortTreeAdapter DataPortTreeAdapter { get; set; } = default!;
    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;
    [Inject] private DiagramService DiagramService { get; set; } = default!;
    [Inject] private DragService DragService { get; set; } = default!;
    [Inject] private IFbSettingsEditorRequest FbSettingsEditorRequest { get; set; } = default!;
    [Inject] private LinkDestinationDialogService LinkDestinationDialogService { get; set; } = default!;
    [Inject] private IContextMenuRequest<NodeEditorContextMenuContext> NodeEditorContextMenuRequest { get; set; } = default!;
    [Inject] private IPropertyGridController<DataflowToolbarPropertyGridContext> PropertyGridController { get; set; } = default!;
    [Inject] private SelectionManager SelectionManager { get; set; } = default!;
    [Inject] private ToolbarService ToolbarService { get; set; } = default!;
    [Inject] private TooltipService TooltipService { get; set; } = default!;

    [Parameter] public BlockNode? Node { get; set; }

    public void Dispose()
    {
        Node!.Changed -= OnNodeChanged;
        LinkDestinationDialogService.ConnectorSelected -= OnLinkDestinationDialogConnectorSelected;
        DiagramEventService.BlockNodesUpdateRequested -= OnBlockNodesUpdateRequestedAsync;

        GC.SuppressFinalize(this);
    }

    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg)
        => callback.InvokeAsync(arg);

    private bool HasDetailedRunModeSettings()
    {
        if (_block.IsFunctionBlock)
            return ((FunctionBlockNode)Node!).RunModeText == "Y";

        return false;
    }

    private bool IsImageVisible()
    {
        var headerConnectorsHaveDraggingLinkType = Node!.Connectors!.Any(
            c => c.Any(c => c is not null && c.IsSystemConnector && c.IsValidDropTarget));
        var headerConnectorsSelected = SelectionManager.SelectedConnectors.Any(
            c => c.Node!.Id == Node.Id && c.IsSystemConnector);
        return !headerConnectorsHaveDraggingLinkType && !headerConnectorsSelected;
    }

    private void OnBlockContainerPointerDown(MouseEventArgs _)
        => TooltipService.StopTooltip();

    private void OnBlockContainerPointerEnter(MouseEventArgs e)
    {
        if (DiagramService.DiagramState.SimplifiedView && !_isHovered)
        {
            _isHovered = true;
            InvokeAsync(StateHasChanged);
        }

        if (_block.IsFunctionBlock || !string.IsNullOrWhiteSpace(_block.Description))
        {
            TooltipService.StartTooltip(TooltipBlockData.GetBlockTooltipInfo(e, _block, Node!, BoundsService.GetDiagramBounds()));
        }
    }

    private void OnBlockContainerPointerLeave(MouseEventArgs _)
    {
        if (DiagramService.DiagramState.SimplifiedView && _isHovered)
        {
            _isHovered = false;
            InvokeAsync(StateHasChanged);
        }

        if (_block.IsFunctionBlock || !string.IsNullOrWhiteSpace(_block.Description))
        {
            TooltipService.StopTooltip();
        }
    }

    private async void OnBlockNodesUpdateRequestedAsync()
        => await InvokeAsync(StateHasChanged);

    private async Task OnConnectorContextMenuRequestedAsync(MouseEventArgs e, BlockNodeConnector connector)
    {
        if (DiagramEventService.ContextMenuAllowed())
        {
            if (!connector.Selected)
                SelectionManager.SetSelection(connector);

            await BlockNodeConnectorContextMenuRequest.SendAsync(new()
            {
                ItemFilter = SelectionManager.GetContextMenuItemFilterForSelection(),
                MouseEventArgs = e
            });
        }
    }

    private async Task OnConnectorDblClickAsync(MouseEventArgs e, BlockNodeConnector connector)
    {
        var conModel = Datastore.DataflowDiagramMapping.GetModel(connector);

        if (_block.IsFunctionBlock)
        {
            ToolbarService.RequestDataflowToolbarSection(DataflowToolbarSection.Properties);
            return;
        }

        if (e.CtrlKey)
            return;

        var underlyingConnector = conModel.GetUnderlyingConnector();
        var container = Datastore.DataflowDiagramMapping.GetModel((ChildContainerNode)Node!);
        var fbConnector = container.FunctionBlocks.Find(x => x.GetConnectors().Contains(underlyingConnector)) is not null ? underlyingConnector : null;

        ContainerConnector? containerConnector = null;
        if (fbConnector is null)
            containerConnector = container.Containers.Find(x => x.GetConnector(underlyingConnector) is not null)?.GetConnector(underlyingConnector) ?? null;

        if (fbConnector is null && containerConnector is null)
            return;

        await Datastore.LoadContainer(container, DiagramService);

        SelectionManager.DeselectAll();

        if (fbConnector is not null)
        {
            SelectionManager.Select(Datastore.DataflowDiagramMapping.GetDiagramModel(fbConnector));

            var fbNode = Datastore.DataflowDiagramMapping.GetDiagramModel(fbConnector.FunctionBlock);
            if (!DiagramService.Diagram.IsNodeInViewport(fbNode))
                DiagramService.Diagram.PanToNode(fbNode);
        }
        else if (containerConnector is not null)
        {
            SelectionManager.Select(Datastore.DataflowDiagramMapping.GetDiagramModel(containerConnector.Connector));

            var containerNode = Datastore.DataflowDiagramMapping.GetDiagramModel(containerConnector.Container);
            if (!DiagramService.Diagram.IsNodeInViewport(containerNode))
                DiagramService.Diagram.PanToNode(containerNode);
        }
    }

    private async Task OnContainerMarkerDblClickAsync()
    {
        if (Datastore.ActiveContainer is not ChildContainer container)
            return;

        await Datastore.LoadContainer(container.Parent, DiagramService);

        var containerNode = Datastore.DataflowDiagramMapping.GetDiagramModel(container);
        SelectionManager.SetSelection(containerNode);
        if (!DiagramService.Diagram.IsNodeInViewport(containerNode))
            DiagramService.Diagram.PanToNode(containerNode);
    }

    private async Task OnContextMenuAsync(MouseEventArgs e)
    {
        if (DiagramEventService.InvokeContextMenuAllowed())
        {
            await NodeEditorContextMenuRequest.SendAsync(new()
            {
                Block = _block,
                ItemFilter = SelectionManager.GetContextMenuItemFilterForSelection(),
                MouseEventArgs = e,
                ObjectOpenedOn = Node
            });
        }
    }

    private void OnDataPortMarkerPointerEnter(MouseEventArgs e, BlockNodeConnector connector)
        => TooltipService.StartTooltip(TooltipDataPortData.GetDataPortMarkerTooltipInfo(Datastore, e, connector, BoundsService.GetDiagramBounds()));

    private void OnDataPortMarkerPointerLeave(MouseEventArgs _)
        => TooltipService.StopTooltip();

    private async Task OnDoubleClickAsync(MouseEventArgs e)
    {
        // Nicht ausführen wenn Ctrl gedrückt gehalten wird um nicht mit "gesichertem" Multiselect zu interferieren
        if (e.CtrlKey)
            return;

        if (_block.IsChildContainer)
            await Datastore.LoadContainer(_block.ChildContainer!, DiagramService);
        else if (_block.IsFunctionBlock)
            await FbSettingsEditorRequest.SendAsync();
    }

    private void OnDrop(BlockNodeConnector blockNodeConnector)
    {
        if (blockNodeConnector.IsValidDropTarget)
        {
            var targetConnector = Datastore.DataflowDiagramMapping.GetModel(blockNodeConnector);

            foreach (var draggedItem in DragService.DraggedItems)
            {
                if (draggedItem is not DataPortNodeModel treeNode)
                    continue;

                var dataPortTreeNode = Datastore.Builder.Cache.DataPortTreeNodeGuids.GetValueOrDefault(treeNode.Id.Value);
                if (dataPortTreeNode is null)
                    continue;

                var connector = targetConnector is ContainerConnector ? targetConnector.GetUnderlyingConnector() : (Connector)targetConnector;
                if (Datastore.Builder.Editors.DataPortTreeNode.CanAssignConnector(dataPortTreeNode, connector))
                    Datastore.Builder.Editors.DataPortTreeNode.AssignConnector(dataPortTreeNode, connector);
            }
        }
    }

    private void OnEngineDisplayTextPointerEnter(MouseEventArgs e)
    {
        var engines = _block.GetEngines(out var containsUnassignedBlocks).ToList();
        if (engines.Count == 0 && !containsUnassignedBlocks)
            return;

        TooltipService.StartTooltip(TooltipEngineData.GetEngineTooltipInfo(e, Node!, engines, containsUnassignedBlocks, BoundsService.GetDiagramBounds()));
    }

    private void OnEngineDisplayTextPointerLeave(MouseEventArgs _)
        => TooltipService.StopTooltip();

    protected override void OnInitialized()
    {
        ArgumentNullException.ThrowIfNull(Diagram, nameof(Diagram));
        ArgumentNullException.ThrowIfNull(Node, nameof(Node));

        _block = new(Datastore, Node);

        Node.Changed += OnNodeChanged;
        LinkDestinationDialogService.ConnectorSelected += OnLinkDestinationDialogConnectorSelected;
        DiagramEventService.BlockNodesUpdateRequested += OnBlockNodesUpdateRequestedAsync;
    }

    private async void OnLinkDestinationDialogConnectorSelected(Connector selectedConnector, ConnectorMarkerType markerType)
    {
        switch (markerType)
        {
            case ConnectorMarkerType.None:
                await ConnectorService.ShowAndSelectConnector(selectedConnector);
                break;
            case ConnectorMarkerType.DataPort:
                await ConnectorService.ShowAndSelectDataPortConnectorMarker(selectedConnector);
                break;
            case ConnectorMarkerType.Published:
                await ConnectorService.ShowAndSelectPublishedConnectorMarker(selectedConnector);
                break;
            default:
                break;
        }
    }

    private void OnMarkerClick(bool controlKeyPressed, ConnectorMarker connectorMarker)
    {
        if (controlKeyPressed)
        {
            if (SelectionManager.SelectedBlockNodes.Any()
                || SelectionManager.SelectedConnectors.Any()
                || SelectionManager.SelectedLabels.Any()
                || SelectionManager.SelectedLinks.Any())
            {
                return;
            }

            if (SelectionManager.IsSelected(connectorMarker))
                SelectionManager.Deselect(connectorMarker);
            else
                SelectionManager.Select(connectorMarker);
        }
        else
        {
            SelectionManager.DeselectAll();
            SelectionManager.Select(connectorMarker);
        }
    }

    private async Task OnMarkerDoubleClickAsync(ConnectorMarker connectorMarker)
    {
        if (connectorMarker.Links.Count == 0)
            return;

        if (connectorMarker.Links.Count == 1)
        {
            if (connectorMarker.Links[0].SourceConnector is null || connectorMarker.Links[0].DestinationConnector is null)
            {
                var dataPortTreeNode = connectorMarker.Connector.IsInput
                    ? connectorMarker.Links[0].SourceDataPortTreeNode!
                    : connectorMarker.Links[0].DestinationDataPortTreeNode!;

                ShowAndSelectDataPort(dataPortTreeNode);
            }
            else
            {
                Connector targetConnector = connectorMarker.Connector.IsInput
                    ? connectorMarker.Links[0].SourceConnector!
                    : connectorMarker.Links[0].DestinationConnector!;

                await ConnectorService.ShowAndSelectConnector(targetConnector);
            }
        }
        else
        {
            ShowLinkDestinationDetailDialog(connectorMarker);
        }
    }

    private void OnNodeChanged(Model _)
        => InvokeAsync(StateHasChanged);

    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Node, nameof(Node));

        if (Node != _node)
        {
            _node = Node;
            _block = new(Datastore, _node);
        }
    }

    private void OnParentContainerMarkerPointerEnter(MouseEventArgs e, BlockNodeConnector connector)
        => TooltipService.StartTooltip(TooltipConnectorData.GetParentConnectorTooltipInfo(Datastore.Builder, e, connector, BoundsService.GetDiagramBounds()));

    private void OnParentContainerMarkerPointerLeave(MouseEventArgs _)
        => TooltipService.StopTooltip();

    private void OnPortContainerPointerEnter(MouseEventArgs e, BlockNodeConnector connector)
        => TooltipService.StartTooltip(TooltipConnectorData.GetConnectorTooltipInfo(Datastore.Builder, e, connector, BoundsService.GetDiagramBounds()));

    private void OnPortContainerPointerLeave(MouseEventArgs _)
    {
        DiagramService.SetDraggingLinkActive();

        TooltipService.StopTooltip();
    }

    private void OnPublishMarkerPointerEnter(MouseEventArgs e, BlockNodeConnector connector)
    {
        var count = connector.PublishedConnectorMarker.Links.Count;
        if (count == 0)
            return;

        TooltipService.StartTooltip(TooltipPublishMarkerData.GetPublishMarkerTooltipInfo(e, connector, BoundsService.GetDiagramBounds()));
    }

    private void OnPublishMarkerPointerLeave(MouseEventArgs _, BlockNodeConnector connector)
    {
        var count = connector.PublishedConnectorMarker.Links.Count;
        if (count == 0)
            return;

        TooltipService.StopTooltip();
    }

    private void OnTitleDblClick(MouseEventArgs _)
    {
        ToolbarService.RequestDataflowToolbarSection(DataflowToolbarSection.Properties);
        PropertyGridController.FocusProperty(nameof(INamedContainerChild.Name));
    }

    private bool ShouldDisplayAllConnectors()
        => !DiagramService.DiagramState.SimplifiedView ||
           Node!.DisplayAllConnectors ||
           Node.Selected ||
           _isHovered;

    private void ShowAndSelectDataPort(DataPortTreeNode dataPortTreeNode)
    {
        var treeNode = DataPortTreeAdapter.GetTreeNode(dataPortTreeNode);

        if (treeNode is null)
            return;

        ToolbarService.RequestDataflowToolbarSection(DataflowToolbarSection.DataPorts);
        treeNode.ScrollToNode(DataPortTreeAdapter.Builder);
    }

    private void ShowLinkDestinationDetailDialog(ConnectorMarker connectorMarker)
    {
        LinkDestinationDialogService.SetSourceConnectorMarker(connectorMarker);
        LinkDestinationDialogService.SetVisibility(true);
    }
}
