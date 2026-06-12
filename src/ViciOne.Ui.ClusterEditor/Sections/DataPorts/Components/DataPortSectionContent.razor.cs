using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazor.Diagrams.Core.Geometry;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.Blazor.Components.Dialog.Components;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.ContextMenu;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Components;

public sealed partial class DataPortSectionContent : ComponentBase, IDisposable
{
    private IEnumerable<DataPortContextMenuItem> _addDataPortContextMenuItems = [];
    private Action _confirmDeleteDialogAction = () => { };
    private Dialog? _confirmDeleteDialogRef;
    private DataPortEditTemplateContext? _editTemplateContext;
    private int _elementsAddedWhileFiltered;
    private bool _groupingButtonsEnabled;
    private readonly List<ITreeNode> _highlightedNodes = [];
    private readonly string _plusIconCssClass = MonochromeIconName.PlusSlim.GetCssClasses(MonochromeIconSize.SmallMedium).ToSpaceSeparated();
    private string _searchText = string.Empty;
    private readonly TreeEditor.Builder.TreeBuilder _treeBuilder = new();

    [Inject] private IContextMenuRequest<AddDataPortContextMenuContext> AddDataPortContextMenuRequest { get; set; } = default!;
    [Inject] private ConnectorService ConnectorService { get; set; } = default!;
    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private DragService DragService { get; set; } = default!;
    [Inject] private LinkDestinationDialogService LinkDestinationDialogService { get; set; } = default!;
    [Inject] private ILogger<DataPortSectionContent> Logger { get; set; } = default!;
    [Inject] private IRulesetProvider RulesetProvider { get; set; } = default!;
    [Inject] private ToolbarService ToolbarService { get; set; } = default!;
    [Inject] private DataPortTreeAdapter TreeAdapter { get; set; } = default!;

    private void CreateEditTemplateContext()
    {
        if (_editTemplateContext is not null)
        {
            _editTemplateContext.Cancel -= OnPropertyEditCancel;
            _editTemplateContext.Confirm -= OnPropertyEditConfirm;
        }

        _editTemplateContext = new();
        _editTemplateContext.Cancel += OnPropertyEditCancel;
        _editTemplateContext.Confirm += OnPropertyEditConfirm;
    }

    public void Dispose()
    {
        Datastore.BuilderChanged -= OnBuilderChangedAsync;
        Datastore.ForcedRefreshRequested -= OnBuilderChangedAsync;
        DragService.DraggingEnded -= OnConnectorDraggingEnded;
        DragService.DraggingStarted -= OnConnectorDraggingStarted;
        LinkDestinationDialogService.DataPortTreeNodeSelected -= OnDataPortTreeNodeSelected;

        TreeAdapter.OnDeleteNodeUserConfirmationRequest = null;
        TreeAdapter.DataPortWithLinksDoubleClicked -= OnDataPortWithLinksDoubleClickedAsync;
        _treeBuilder.Notifications.RootNodesUpdated -= OnRootNodesUpdated;
        _treeBuilder.Dispose();

        if (_editTemplateContext is not null)
        {
            _editTemplateContext.Cancel -= OnPropertyEditCancel;
            _editTemplateContext.Confirm -= OnPropertyEditConfirm;
        }
    }

    private void FilterNodes()
    {
        TreeAdapter.FilterNodes(_searchText);

        if (!_treeBuilder.Filter.IsFilterActive)
            _elementsAddedWhileFiltered = 0;
    }

    private void GetPossibleTargetNodes(ITreeNode parentNode, Connector connector, List<ITreeNode> nodesToHighlight)
    {
        if (parentNode is DataPortChildNodeModel childNode && childNode.Parent is not DataPortRootNodeModel)
        {
            var dataPortTreeNode = Datastore.Builder.Cache.DataPortTreeNodeIds[childNode.Id.Value];

            if (childNode.TransferDirectionIsPossible(connector) &&
                Datastore.Builder.Editors.DataPortTreeNode.CanAssignConnector(dataPortTreeNode, connector) &&
                !childNode.IsEditModeActive)
            {
                nodesToHighlight.Add(childNode);
            }
        }

        foreach (var node in TreeAdapter.GetChildren(parentNode))
            GetPossibleTargetNodes(node, connector, nodesToHighlight);
    }

    private async Task OnAddDataPortClickedAsync(MouseEventArgs args)
    {
        await RefreshPossibleDataPortsAsync();
        await AddDataPortContextMenuRequest.SendAsync(new AddDataPortContextMenuContext
        {
            AddDataPortContextMenuItems = _addDataPortContextMenuItems,
            MouseEventArgs = args
        });
    }

    private void OnAddDataPortContextMenuItemClick(string dataPortKey)
    {
        TreeAdapter.CreateNewDataPortRootNode(dataPortKey);

        if (_treeBuilder.Filter.IsFilterActive)
            _elementsAddedWhileFiltered++;
    }

    private async void OnBuilderChangedAsync()
    {
        CreateEditTemplateContext();
        TryInitDataPortTree();
        await UpdateGroupingButtonState();
        await RefreshPossibleDataPortsAsync();
    }

    private void OnCollapseAllGroups()
        => _treeBuilder.Expansion.ChangeExpansionForLayers(false);

    private void OnConnectorDraggingEnded(Point _)
    {
        DragService.DraggingEnded -= OnConnectorDraggingEnded;

        _treeBuilder.DragAndDrop.InboundDropped -= OnTreeEditorExternalDrop;
        TreeAdapter.ValidInboundDropTargets.Clear();
        _treeBuilder.DragAndDrop.EndInboundDrag();

        if (_highlightedNodes.Count != 0)
        {
            TreeAdapter.SetHighlightState(false, [.. _highlightedNodes]);
            _highlightedNodes.Clear();
        }
    }

    private void OnConnectorDraggingStarted()
    {
        if (DragService.DraggedItems.FirstOrDefault(d => d is BlockNodeConnector) is not BlockNodeConnector draggedBlockNodeConnector)
            return;

        var connector = draggedBlockNodeConnector.Connector.GetUnderlyingConnector();
        if (connector is null)
            return;

        foreach (var rootNode in TreeAdapter.GetRootNodes())
            GetPossibleTargetNodes(rootNode, connector, _highlightedNodes);

        DragService.DraggingEnded += OnConnectorDraggingEnded;

        TreeAdapter.SetHighlightState(true, [.. _highlightedNodes]);
        TreeAdapter.ValidInboundDropTargets.AddRange(_highlightedNodes);
        _treeBuilder.DragAndDrop.StartInboundDrag(DropZone.Insert);
        _treeBuilder.DragAndDrop.InboundDropped += OnTreeEditorExternalDrop;
    }

    private void OnDataPortTreeNodeSelected(DataPortTreeNode dataPortTreeNode)
    {
        var treeNode = TreeAdapter.GetTreeNode(dataPortTreeNode);
        if (treeNode is null)
            return;

        ToolbarService.RequestDataflowToolbarSection(DataflowToolbarSection.DataPorts);
        treeNode.ScrollToNode(TreeAdapter.Builder);
    }

    private async void OnDataPortWithLinksDoubleClickedAsync(ITreeNode node)
    {
        var dataPortTreeNode = Datastore.Builder.Cache.DataPortTreeNodeIds[((GuidNodeIdentifier)node.Id).Value];
        var linksCount = dataPortTreeNode?.Links.Count() ?? 0;
        if (linksCount == 0)
            return;

        if (linksCount == 1)
        {
            var link = dataPortTreeNode!.Links.First();
            Connector connectorToHighlight = link.SourceConnector is not null
                ? link.SourceConnector!
                : link.DestinationConnector!;

            await ConnectorService.ShowAndSelectDataPortConnectorMarker(connectorToHighlight);
        }
        else
        {
            LinkDestinationDialogService.SetSourceDataPortTreeNode(dataPortTreeNode);
            LinkDestinationDialogService.SetVisibility(true);
        }
    }

    private async Task OnDeleteNodeCancelAsync()
    {
        if (_confirmDeleteDialogRef is null)
            return;

        await _confirmDeleteDialogRef.CloseAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnDeleteNodeConfirmAsync()
    {
        if (_confirmDeleteDialogAction is not null)
            _confirmDeleteDialogAction();

        if (_confirmDeleteDialogRef is null)
            return;

        await _confirmDeleteDialogRef.CloseAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async void OnDeleteNodeUserConfirmationRequestAsync(ITreeNode node, Action action)
    {
        if (node is not DataPortNodeModel treeNode || treeNode.Children.Count == 0)
        {
            action();
            return;
        }

        _confirmDeleteDialogAction = action;

        if (_confirmDeleteDialogRef is not null)
            await _confirmDeleteDialogRef.ShowAsync();
    }

    private void OnExpandAllGroups()
        => _treeBuilder.Expansion.ChangeExpansionForLayers(true);

    protected override async Task OnInitializedAsync()
    {
        Datastore.BuilderChanged += OnBuilderChangedAsync;
        Datastore.ForcedRefreshRequested += OnBuilderChangedAsync;
        DragService.DraggingStarted += OnConnectorDraggingStarted;
        LinkDestinationDialogService.DataPortTreeNodeSelected += OnDataPortTreeNodeSelected;

        _treeBuilder.SetAdapter(TreeAdapter);
        TreeAdapter.Initialize();
        TreeAdapter.OnDeleteNodeUserConfirmationRequest = OnDeleteNodeUserConfirmationRequestAsync;
        TreeAdapter.DataPortWithLinksDoubleClicked += OnDataPortWithLinksDoubleClickedAsync;

        _treeBuilder.Notifications.RootNodesUpdated += OnRootNodesUpdated;

        // If the component gets initialized after cluster was loaded
        if (!Datastore.HasBuilder)
            return;

        CreateEditTemplateContext();
        await UpdateGroupingButtonState();

        await RefreshPossibleDataPortsAsync();
    }

    private void OnPossibleChildNodeClicked(DataPortNodeModel parentNode, DataPortChildNodeModel childNode)
    {
        TreeAdapter.CreateNewChildNode(parentNode, childNode);
        _treeBuilder.Notifications.NotifyNodeChanged(parentNode, ChangedNodeDetail.None);

        if (!parentNode.Expanded)
            _treeBuilder.Expansion.ChangeExpansion(parentNode, true);
    }

    private void OnPropertyEditCancel(DataPortNodeModel node)
    {
        node.IsEditModeActive = false;

        if (node is DataPortChildNodeModel childNode)
            TreeAdapter.RevertNodeChanges(childNode);

        _treeBuilder.Notifications.NotifyNodeChanged(node, ChangedNodeDetail.None);
    }

    private void OnPropertyEditConfirm(DataPortNodeModel node)
    {
        node.IsEditModeActive = false;
        TreeAdapter.ProcessNodeChanges(node);
        TreeAdapter.SortNodeChildren(node);
        _treeBuilder.Notifications.NotifyNodeChanged(node, ChangedNodeDetail.Icons);
    }

    private async void OnRootNodesUpdated()
        => await UpdateGroupingButtonState();

    private void OnTreeEditorExternalDrop(ITreeNode nodeDroppedOn, DropZone dropZone)
    {
        if (dropZone != DropZone.Insert)
            return;

        DragService.EndDragging();

        if (DragService.DraggedItems.FirstOrDefault(d => d is BlockNodeConnector) is not BlockNodeConnector draggedBlockNodeConnector)
            return;

        var connector = draggedBlockNodeConnector.Connector.GetUnderlyingConnector();
        var dataPortTreeNode = Datastore.Builder.Cache.DataPortTreeNodeIds[((GuidNodeIdentifier)nodeDroppedOn.Id).Value];

        if (connector is null || dataPortTreeNode is null)
            return;

        if (Datastore.Builder.Editors.DataPortTreeNode.CanAssignConnector(dataPortTreeNode, connector))
            Datastore.Builder.Editors.DataPortTreeNode.AssignConnector(dataPortTreeNode, connector);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to reassign builder with cluster id:{ClusterId} v{Version}.")]
    public static partial void ReassignBuilderFailed(ILogger logger, Exception ex, Guid ClusterId, Version Version);

    private async Task RefreshPossibleDataPortsAsync()
    {
        _addDataPortContextMenuItems = RulesetProvider
            .GetRulesetIdentifiers(DataPortTreeAdapter.DataPortCategory)
            .Select(r => (r.Key, RulesetProvider.GetRuleset(r)))
            .Where(item => item.Item2?.Root is not null)
            .Select(item =>
            {
                var root = item.Item2.Root!;
                var rootNode = TreeAdapter.GetDataPortRootNode(root.Id);
                rootNode?.PossibleChildren = [.. rootNode.GetPossibleChildNodes()];

                var enabled = rootNode?.PossibleChildren.Any() ?? true;
                return new DataPortContextMenuItem(root.Name, enabled, item.Key);
            })
            .OrderBy(i => i.DisplayText);

        await InvokeAsync(StateHasChanged);
    }

    private void TryInitDataPortTree()
    {
        try
        {
            TreeAdapter.InitializeDataPortTree();
        }
        catch (Exception ex)
        {
            ReassignBuilderFailed(Logger, ex, Datastore.Builder.Cluster.Id, Datastore.Builder.Cluster.Version);
        }
    }

    private async Task UpdateGroupingButtonState()
    {
        _groupingButtonsEnabled = TreeAdapter.GetRootNodes().Any();
        await InvokeAsync(StateHasChanged);
    }
}
