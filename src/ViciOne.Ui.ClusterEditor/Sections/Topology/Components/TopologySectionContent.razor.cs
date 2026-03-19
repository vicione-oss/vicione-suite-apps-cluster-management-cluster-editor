using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ViciOne.Ui.ClusterEditor.Models.Contexts;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Models;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;
using ViciOne.Ui.Shared.Dx.Components;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace ViciOne.Ui.ClusterEditor.Sections.Topology.Components;

public sealed partial class TopologySectionContent : IDisposable
{
    private Action _confirmDeleteDialogAction = () => { };
    private DxDialog? _confirmDeleteDialogRef;
    private readonly TopologyEditTemplateContext _editTemplateContext = new();
    private int _elementsAddedWhileFiltered;
    private bool _groupingButtonsEnabled;
    private readonly string _plusIconCssClass = MonochromeIconName.PlusSlim.GetCssClasses(MonochromeIconSize.SmallMedium).ToSpaceSeparated();
    private string _searchText = string.Empty;
    private readonly TreeEditor.Builder.TreeBuilder _treeBuilder = new();

    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private ILogger<TopologySectionContent> Logger { get; set; } = default!;
    [Inject] private TopologyTreeAdapter TreeAdapter { get; set; } = default!;

    public void Dispose()
    {
        Datastore.BuilderChanged -= OnBuilderChanged;
        TreeAdapter.OnDeleteNodeUserConfirmationRequest = null;

        _editTemplateContext.Cancel -= OnPropertyEditCancel;
        _editTemplateContext.Confirm -= OnPropertyEditConfirm;

        _treeBuilder.Notifications.RootNodesUpdated -= OnTreeBuilderRootNodesUpdated;
        _treeBuilder.Dispose();
    }

    private void FilterNodes()
    {
        TreeAdapter.FilterNodes(_searchText);

        if (!_treeBuilder.Filter.IsFilterActive)
            _elementsAddedWhileFiltered = 0;
    }

    [LoggerMessage(1, LogLevel.Error, "Failed to reassign builder with cluster id:{ClusterId} v{Version}", EventName = "ReassignBuilderFailed")]
    public static partial void LogTreeAdapterException(ILogger logger, Exception ex, Guid ClusterId, Version Version);

    private void OnAddNodeGroupClicked()
    {
        TreeAdapter.AddTopologyNodeGroup();

        if (_treeBuilder.Filter.IsFilterActive)
            _elementsAddedWhileFiltered++;
    }

    private void OnBuilderChanged()
        => TryInitTreeAdapter();

    private void OnCollapseAllGroups()
        => _treeBuilder.Expansion.ChangeExpansionForLayers(false);

    private async Task OnDeleteNodeCancel()
    {
        if (_confirmDeleteDialogRef is null)
            return;

        await _confirmDeleteDialogRef.CloseAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnDeleteNodeConfirm()
    {
        if (_confirmDeleteDialogAction is not null)
            _confirmDeleteDialogAction();

        if (_confirmDeleteDialogRef is null)
            return;

        await _confirmDeleteDialogRef.CloseAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async void OnDeleteNodeUserConfirmationRequest(ITreeNode node, Action action)
    {
        if (node is not TopologyTreeViewModel model || model.Children.Count == 0)
        {
            action();
            return;
        }

        _confirmDeleteDialogAction = action;
        await InvokeAsync(_confirmDeleteDialogRef!.OpenAsync);
    }

    private void OnExpandAllGroups()
        => _treeBuilder.Expansion.ChangeExpansionForLayers(true);

    protected override void OnInitialized()
    {
        Datastore.BuilderChanged += OnBuilderChanged;

        _treeBuilder.Notifications.RootNodesUpdated += OnTreeBuilderRootNodesUpdated;
        _treeBuilder.SetAdapter(TreeAdapter);
        TreeAdapter.OnDeleteNodeUserConfirmationRequest = OnDeleteNodeUserConfirmationRequest;

        _editTemplateContext.Cancel += OnPropertyEditCancel;
        _editTemplateContext.Confirm += OnPropertyEditConfirm;

        // If the component gets initialized after cluster was loaded
        if (!Datastore.HasBuilder)
            return;

        TryInitTreeAdapter();
    }

    private void OnPropertyEditCancel(TopologyTreeViewModel node)
    {
        // We don't have to care because changes were made to a copy and the DataItem is untouched
        node.IsEditModeActive = false;
        _treeBuilder.Notifications.NotifyNodeChanged(node, ChangedNodeDetail.None);
    }

    private void OnPropertyEditConfirm(TopologyTreeViewModel model, object? editItem)
    {
        if (editItem is null)
            return;

        // Edit item is a shallow copy of the orginal DataItem that contains the changes
        TreeAdapter.ProcessNodeChange(model, editItem);

        model.IsEditModeActive = false;
        _treeBuilder.Notifications.NotifyNodeChanged(model, ChangedNodeDetail.None);
    }

    private void OnTreeBuilderRootNodesUpdated()
    {
        _groupingButtonsEnabled = TreeAdapter.GetRootNodes().Any();
        InvokeAsync(StateHasChanged);
    }

    private void TryInitTreeAdapter()
    {
        try
        {
            TreeAdapter.BuildClusterTree(Datastore.Builder);
        }
        catch (Exception ex)
        {
            LogTreeAdapterException(Logger, ex, Datastore.Builder.Cluster.Id, Datastore.Builder.Cluster.Version);
        }

        // We have to call the event handler manually here
        // The RootNodesUpdated event is always called with empty root nodes when the component is initialized
        // If BuilderClusterTree() here adds root nodes, the RootNodesUpdated event is not triggered
        // Trying to call Builder.Notifications.NotifyRootNodesChanged(); doesn't help either, looks like some
        // caching or buffering issue
        OnTreeBuilderRootNodesUpdated();
    }
}
