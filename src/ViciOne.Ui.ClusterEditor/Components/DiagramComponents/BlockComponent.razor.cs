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
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components.DiagramComponents;

[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling", Justification = "#1599")]
public sealed partial class BlockComponent : ComponentBase, IDisposable, IHandleEvent
{
    private Block _block = new();
    private readonly object _blockTooltipKey = new();
    private readonly object _connectorTooltipKey = new();
    private readonly object _dataPortMarkerTooltipKey = new();
    private readonly object _engineTooltipKey = new();
    private bool? _hasDropTargets;
    private bool _isDirty = true;
    private bool _isHovered;
    private bool _isImageVisible = true;
    private BlockNode? _node;
    private readonly object _parentMarkerTooltipKey = new();
    private readonly object _publishMarkerTooltipKey = new();

    [CascadingParameter] internal Diagram? Diagram { get; set; }
    [Inject] private IContextMenuRequest<BlockNodeConnectorContextMenuContext> BlockNodeConnectorContextMenuRequest { get; set; } = default!;
    [Inject] private BoundsService BoundsService { get; set; } = default!;
    [Inject] private ConnectorService ConnectorService { get; set; } = default!;
    [Inject] private DataPortTreeAdapter DataPortTreeAdapter { get; set; } = default!;
    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;
    [Inject] private DiagramService DiagramService { get; set; } = default!;
    [Inject] private DragService DragService { get; set; } = default!;
    [Inject] private IFbSettingsEditorRequest FbSettingsEditorRequest { get; set; } = default!;
    [Inject] private LinkDestinationDialogService LinkDestinationDialogService { get; set; } = default!;
    [Inject] private IContextMenuRequest<NodeEditorContextMenuContext> NodeEditorContextMenuRequest { get; set; } = default!;
    [Inject] private IPropertyGridController<DataflowToolbarPropertyGridContext> PropertyGridController { get; set; } = default!;
    [Inject] private PublishedConnectorsService PublishedConnectorsService { get; set; } = default!;
    [Inject] private SelectionManager SelectionManager { get; set; } = default!;
    [Inject] private ToolbarService ToolbarService { get; set; } = default!;
    [Inject] private TooltipService TooltipService { get; set; } = default!;
    [Parameter] public BlockNode? Node { get; set; }

    private void AssignConnector(DataPortChildNodeModel treeNode, Connector connector)
    {
        // Several nodes can be dragged onto one connector and the drop target only has to suit one
        // of them, so every node is asked again for itself.
        if (!treeNode.TransferDirectionIsPossible(connector))
            return;

        var dataPortTreeNode = Datastore.Builder.Cache.DataPortTreeNodeIds.GetValueOrDefault(treeNode.Id.Value);
        if (dataPortTreeNode is null)
            return;

        if (Datastore.Builder.Editors.DataPortTreeNode.CanAssignConnector(dataPortTreeNode, connector))
            Datastore.Builder.Editors.DataPortTreeNode.AssignConnector(dataPortTreeNode, connector);
    }

    public void Dispose()
    {
        Node!.Changed -= OnNodeChanged;
        LinkDestinationDialogService.ConnectorSelected -= OnLinkDestinationDialogConnectorSelected;
        DiagramEventService.BlockNodesUpdateRequested -= OnBlockNodesUpdateRequestedAsync;
        SelectionManager.ConnectorSelectionChanged -= OnConnectorSelectionChanged;

        StopAllTooltips();

        GC.SuppressFinalize(this);
    }

    private static (MarkerLine? Outer, MarkerLine? Inner) GetMarkerLines(BlockNodeConnector connector)
    {
        var published = connector.PublishedConnectorMarker;
        var dataPort = connector.DataPortConnectorMarker;

        var baseLength = published.Visible ? 15 :
            dataPort.Visible ? 8 :
            connector.IsOnContainer ? 2 : 0;

        if (baseLength == 0)
            return (null, null);

        var selectedLength = published.Selected ? 15 : dataPort.Selected ? 8 : 0;
        var tracedLength = published.Traced ? 15 : dataPort.Traced ? 8 : 0;

        var farModifier = selectedLength == baseLength ? "selected"
            : tracedLength == baseLength ? "traced"
            : string.Empty;

        var portModifier = selectedLength > 0 ? "selected"
            : tracedLength > 0 ? "traced"
            : string.Empty;

        var outer = new MarkerLine(baseLength, farModifier);

        if (portModifier == farModifier)
            return (outer, null);

        var innerLength = selectedLength > 0 ? selectedLength : tracedLength;
        return (outer, new MarkerLine(innerLength, portModifier));
    }

    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg)
        => callback.InvokeAsync(arg);

    private void OnBlockContainerPointerDown()
        => TooltipService.StopTooltip(_blockTooltipKey);

    private void OnBlockContainerPointerEnter(PointerEventArgs e)
    {
        if (DiagramService.DiagramState.SimplifiedView && !_isHovered)
        {
            _isHovered = true;
            _isDirty = true;
            InvokeAsync(StateHasChanged);
        }

        if (_block.IsFunctionBlock || !string.IsNullOrWhiteSpace(_block.Description))
        {
            TooltipService.StartTooltip(_blockTooltipKey, TooltipBlockData.GetBlockTooltipInfo(e, _block, Node!, BoundsService.GetDiagramBounds()));
        }
    }

    private void OnBlockContainerPointerLeave()
    {
        if (DiagramService.DiagramState.SimplifiedView && _isHovered)
        {
            _isHovered = false;
            _isDirty = true;
            InvokeAsync(StateHasChanged);
        }

        // Stop all tooltips owned by this block, not just the block tooltip:
        // connectors/markers rendered on hover in Simplified View may have been
        // removed from the DOM (by the re-render above, or by moving onto an
        // overlapping block) before their own pointerleave could fire.
        StopAllTooltips();
    }

    private async void OnBlockNodesUpdateRequestedAsync()
    {
        var hasDropTargets = false;
        foreach (var c in Node!.ConnectorsToList())
        {
            if (c.IsValidDropTarget)
            {
                hasDropTargets = true;
                break;
            }
        }

        RecalculateImageVisible();

        if (hasDropTargets == _hasDropTargets && !_isDirty)
            return;

        _hasDropTargets = hasDropTargets;
        _isDirty = true;
        await InvokeAsync(StateHasChanged);
    }

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
        Connector? fbConnector = null;
        foreach (var fb in container.FunctionBlocks)
        {
            foreach (var conn in fb.GetConnectors())
            {
                if (conn == underlyingConnector)
                {
                    fbConnector = underlyingConnector;
                    break;
                }
            }

            if (fbConnector is not null)
                break;
        }

        ContainerConnector? containerConnector = null;
        if (fbConnector is null)
        {
            foreach (var child in container.Containers)
            {
                containerConnector = child.GetConnector(underlyingConnector);
                if (containerConnector is not null)
                    break;
            }
        }

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

    private void OnConnectorSelectionChanged(IEnumerable<BlockNodeConnector> obj)
    {
        RecalculateImageVisible();

        if (_isDirty)
            InvokeAsync(StateHasChanged);
    }

    private async Task OnContainerMarkerDblClick(BlockNodeConnector connector)
    {
        if (Datastore.ActiveContainer is not ChildContainer container)
            return;

        await Datastore.LoadContainer(container.Parent, DiagramService);

        var containerNode = Datastore.DataflowDiagramMapping.GetDiagramModel(container);
        if (!DiagramService.Diagram.IsNodeInViewport(containerNode))
            DiagramService.Diagram.PanToNode(containerNode);

        if (connector.ParentContainerConnector is not null)
        {
            var diagramModel = Datastore.DataflowDiagramMapping.GetDiagramModel(connector.ParentContainerConnector);
            SelectionManager.SetSelection(diagramModel);
        }
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

    private void OnDataPortMarkerPointerEnter(PointerEventArgs e, BlockNodeConnector connector)
        => TooltipService.StartTooltip(_dataPortMarkerTooltipKey, TooltipDataPortData.GetDataPortMarkerTooltipInfo(Datastore, e, connector, BoundsService.GetDiagramBounds()));

    private void OnDataPortMarkerPointerLeave()
        => TooltipService.StopTooltip(_dataPortMarkerTooltipKey);

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
        if (!blockNodeConnector.IsValidDropTarget)
            return;

        var targetConnector = Datastore.DataflowDiagramMapping.GetModel(blockNodeConnector);
        var connector = targetConnector is ContainerConnector ? targetConnector.GetUnderlyingConnector() : (Connector)targetConnector;

        foreach (var treeNode in DragService.DraggedItems.OfType<DataPortChildNodeModel>())
            AssignConnector(treeNode, connector);
    }

    private void OnEngineDisplayTextPointerEnter(PointerEventArgs e)
    {
        bool containsUnassignedBlocks;
        List<Cluster.Model.Engine> engines = [];
        foreach (var engine in _block.GetEngines(out containsUnassignedBlocks))
            engines.Add(engine);

        if (engines.Count == 0 && !containsUnassignedBlocks)
            return;

        TooltipService.StartTooltip(_engineTooltipKey, TooltipEngineData.GetEngineTooltipInfo(e, Node!, engines, containsUnassignedBlocks, BoundsService.GetDiagramBounds()));
    }

    private void OnEngineDisplayTextPointerLeave()
        => TooltipService.StopTooltip(_engineTooltipKey);

    protected override void OnInitialized()
    {
        ArgumentNullException.ThrowIfNull(Diagram, nameof(Diagram));
        ArgumentNullException.ThrowIfNull(Node, nameof(Node));

        Node.Changed += OnNodeChanged;
        LinkDestinationDialogService.ConnectorSelected += OnLinkDestinationDialogConnectorSelected;
        DiagramEventService.BlockNodesUpdateRequested += OnBlockNodesUpdateRequestedAsync;
        SelectionManager.ConnectorSelectionChanged += OnConnectorSelectionChanged;
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
            if (SelectionManager.SelectedBlockNodes.Count > 0
                || SelectionManager.SelectedConnectors.Count > 0
                || SelectionManager.SelectedLabels.Count > 0
                || SelectionManager.SelectedLinks.Count > 0)
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
        {
            // A marker without links is only rendered for a connector that is published but
            // not linked yet; jump to its entry in the Published Connectors section instead.
            if (connectorMarker.Type == ConnectorMarkerType.Published)
                ShowAndSelectPublishedConnector(connectorMarker.Connector);

            return;
        }

        if (connectorMarker.Links.Count == 1)
        {
            var link = connectorMarker.Links[0];

            if (link.SourceConnector is null || link.DestinationConnector is null)
            {
                var dataPortTreeNode = connectorMarker.Connector.IsInput
                    ? link.SourceDataPortTreeNode!
                    : link.DestinationDataPortTreeNode!;

                ShowAndSelectDataPort(dataPortTreeNode);
            }
            else
            {
                Connector targetConnector = connectorMarker.Connector.IsInput
                    ? link.SourceConnector!
                    : link.DestinationConnector!;

                await ConnectorService.ShowAndSelectConnector(targetConnector);
            }
        }
        else
        {
            ShowLinkDestinationDetailDialog(connectorMarker);
        }
    }

    private void OnNodeChanged(Model _)
    {
        _isDirty = true;
        InvokeAsync(StateHasChanged);
    }

    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Node, nameof(Node));

        if (Node != _node)
        {
            _node = Node;
            _block = new(Datastore, _node);
            _isDirty = true;
        }

        RecalculateImageVisible();
    }

    private void OnParentContainerMarkerPointerEnter(PointerEventArgs e, BlockNodeConnector connector)
        => TooltipService.StartTooltip(_parentMarkerTooltipKey, TooltipConnectorData.GetParentConnectorTooltipInfo(Datastore.Builder, e, connector, BoundsService.GetDiagramBounds()));

    private void OnParentContainerMarkerPointerLeave()
        => TooltipService.StopTooltip(_parentMarkerTooltipKey);

    private void OnPortContainerPointerEnter(PointerEventArgs e, BlockNodeConnector connector)
        => TooltipService.StartTooltip(_connectorTooltipKey, TooltipConnectorData.GetConnectorTooltipInfo(Datastore.Builder, e, connector, BoundsService.GetDiagramBounds()));

    private void OnPortContainerPointerLeave()
    {
        DiagramService.SetDraggingLinkActive();

        TooltipService.StopTooltip(_connectorTooltipKey);
    }

    private void OnPublishMarkerPointerEnter(PointerEventArgs e, BlockNodeConnector connector)
    {
        var count = connector.PublishedConnectorMarker.Links.Count;
        if (count == 0)
            return;

        TooltipService.StartTooltip(_publishMarkerTooltipKey, TooltipPublishMarkerData.GetPublishMarkerTooltipInfo(e, connector, BoundsService.GetDiagramBounds()));
    }

    private void OnPublishMarkerPointerLeave()
        => TooltipService.StopTooltip(_publishMarkerTooltipKey);

    private void OnTitleDblClick()
    {
        ToolbarService.RequestDataflowToolbarSection(DataflowToolbarSection.Properties);
        PropertyGridController.FocusProperty(nameof(INamedContainerChild.Name));
    }

    private void RecalculateImageVisible()
    {
        var isVisible = true;

        foreach (var c in Node!.ConnectorsToList())
        {
            if (c.IsSystemConnector && (c.IsValidDropTarget || c.Selected))
            {
                isVisible = false;
                break;
            }
        }

        if (isVisible == _isImageVisible)
            return;

        _isImageVisible = isVisible;
        _isDirty = true;
    }

    private bool IsConnectorHidden(BlockNodeConnector connector)
        => !ShouldDisplayAllConnectors() && !connector.ShouldDisplayConnector();

    private bool ShouldDisplayAllConnectors()
        => !DiagramService.DiagramState.SimplifiedView ||
           Node!.DisplayAllConnectors ||
           Node.Selected ||
           _isHovered;

    protected override bool ShouldRender()
    {
        if (!_isDirty)
            return false;

        _isDirty = false;
        return true;
    }

    private void ShowAndSelectDataPort(DataPortTreeNode dataPortTreeNode)
    {
        var treeNode = DataPortTreeAdapter.GetTreeNode(dataPortTreeNode);

        if (treeNode is null)
            return;

        ToolbarService.RequestDataflowToolbarSection(DataflowToolbarSection.DataPorts);
        treeNode.ScrollToNode(DataPortTreeAdapter.Builder);
    }

    private void ShowAndSelectPublishedConnector(BlockNodeConnector blockNodeConnector)
    {
        // Publishing always applies to the underlying connector, so a marker on a container maps
        // to the connector of the function block inside it. For a function block connector this
        // resolves to the connector itself.
        var connector = Datastore.DataflowDiagramMapping.GetModel(blockNodeConnector).GetUnderlyingConnector();

        ToolbarService.RequestDataflowToolbarSection(DataflowToolbarSection.Connectors);
        PublishedConnectorsService.RequestPublishedConnectorSelection(connector);
    }

    private void ShowLinkDestinationDetailDialog(ConnectorMarker connectorMarker)
    {
        LinkDestinationDialogService.SetSourceConnectorMarker(connectorMarker);
        LinkDestinationDialogService.SetVisibility(true);
    }

    // Stops every tooltip this block owns. Child elements (connectors, markers)
    // are only in the DOM while hovered in Simplified View; when the block
    // re-renders or the pointer moves onto an overlapping block, those elements
    // are removed WITHOUT firing their own pointerleave, so their tooltips would
    // otherwise leak. The block container's pointerleave is the authoritative
    // cleanup point.
    private void StopAllTooltips()
    {
        TooltipService.StopTooltip(_blockTooltipKey);
        TooltipService.StopTooltip(_connectorTooltipKey);
        TooltipService.StopTooltip(_dataPortMarkerTooltipKey);
        TooltipService.StopTooltip(_engineTooltipKey);
        TooltipService.StopTooltip(_parentMarkerTooltipKey);
        TooltipService.StopTooltip(_publishMarkerTooltipKey);
    }

    private readonly record struct MarkerLine(int Length, string Modifier);
}
