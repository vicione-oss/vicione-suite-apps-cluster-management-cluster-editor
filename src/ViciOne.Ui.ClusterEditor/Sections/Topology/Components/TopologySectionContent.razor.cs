using System;
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
    private readonly string _plusIconCssClass = MonochromeIconName.PlusSlim.GetCssClasses(MonochromeIconSize.SmallMedium).ToSpaceSeparated();
    private readonly TreeEditor.Builder.TreeBuilder _treeBuilder = new();

    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private ILogger<TopologySectionContent> Logger { get; set; } = default!;
    [Inject] private TopologyTreeAdapter TreeAdapter { get; set; } = default!;

    public void Dispose()
    {
        Datastore.BuilderChanged -= OnBuilderChangedAsync;
        TreeAdapter.OnDeleteNodeUserConfirmationRequest = null;

        _editTemplateContext.Cancel -= OnPropertyEditCancel;
        _editTemplateContext.Confirm -= OnPropertyEditConfirm;

        _treeBuilder.Dispose();
    }

    [LoggerMessage(1, LogLevel.Error, "Failed to reassign builder with cluster id:{ClusterId} v{Version}", EventName = "ReassignBuilderFailed")]
    public static partial void LogTreeAdapterException(ILogger logger, Exception ex, Guid ClusterId, Version Version);

    private void OnAddNodeGroupClicked()
    {
        TreeAdapter.AddTopologyNodeGroup();

        if (_treeBuilder.Filter.IsFilterActive)
            _elementsAddedWhileFiltered++;
    }

    private void OnBuilderChangedAsync()
        => TryInitTreeAdapter();

    private void OnCollapseAllGroups()
        => _treeBuilder.Expansion.ChangeExpansionForLayers(false);

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

    private void OnFilterTextChanged(string filterText)
    {
        TreeAdapter.FilterNodes(filterText);

        if (!_treeBuilder.Filter.IsFilterActive)
            _elementsAddedWhileFiltered = 0;
    }

    protected override void OnInitialized()
    {
        Datastore.BuilderChanged += OnBuilderChangedAsync;

        _treeBuilder.SetAdapter(TreeAdapter);
        TreeAdapter.OnDeleteNodeUserConfirmationRequest = OnDeleteNodeUserConfirmationRequestAsync;

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
    }
}
