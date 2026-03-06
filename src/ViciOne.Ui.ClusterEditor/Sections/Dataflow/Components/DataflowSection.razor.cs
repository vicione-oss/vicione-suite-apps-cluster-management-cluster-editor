using System;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Sections.Dataflow.Models;
using ViciOne.Ui.ClusterEditor.Sections.Dataflow.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;
using ViciOne.Ui.Shared.Dx.Components;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace ViciOne.Ui.ClusterEditor.Sections.Dataflow.Components;

public sealed partial class DataflowSection : ComponentBase, IDisposable
{
    private readonly string _addIconCssClass = MonochromeIconName.PlusSlim.GetCssClasses(MonochromeIconSize.SmallMedium).ToSpaceSeparated();
    private DxDialog? _confirmDeleteDialogRef;
    private DataflowStructureTreeNode? _currentDeletingNode;
    private ITreeNode? _currentSelectedNode;
    private int _dataflowsAddedWhileFiltered;
    private readonly TreeEditor.Builder.TreeBuilder _treeBuilder = new();

    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;
    [Inject] private ILogger<DataflowSection> Logger { get; set; } = default!;
    [Inject] private DataflowStructureTreeAdapter TreeAdapter { get; set; } = default!;

    public void Dispose()
    {
        Datastore.BuilderChanged -= OnBuilderChangedAsync;
        DiagramEventService.ContainerLoaded -= OnContainerLoadedAsync;

        TreeAdapter.DeleteStarted -= OnDataflowDeleteStartedAsync;
        _treeBuilder.Selection.SelectionChanged -= OnSelectionChanged;

        _treeBuilder.Dispose();
    }

    private async void OnBuilderChangedAsync()
    {
        TryInitTreeAdapter();
        await InvokeAsync(StateHasChanged);
    }

    private async void OnCancelClickedAsync()
        => await _confirmDeleteDialogRef!.CloseAsync();

    private void OnClosingDataflowDeleteDialog()
    {
        if (_currentDeletingNode is not null)
        {
            _currentDeletingNode.Deleting = false;
            _treeBuilder.Notifications.NotifyNodeChanged(_currentDeletingNode);

            _currentDeletingNode = null;
        }
    }

    private async void OnConfirmDeleteClickedAsync()
    {
        if (_currentDeletingNode is null)
            return;

        Datastore.RemoveDataflow(_currentDeletingNode.Dataflow);
        _currentDeletingNode = null;

        await _confirmDeleteDialogRef!.CloseAsync();
    }

    private async void OnContainerLoadedAsync(Container container)
    {
        if (_currentSelectedNode is not null)
            _treeBuilder.Notifications.NotifyNodeChanged(_currentSelectedNode);

        await InvokeAsync(StateHasChanged);
    }

    private void OnCreateNewDataflowClicked()
    {
        if (_treeBuilder.Filter.IsFilterActive)
            _dataflowsAddedWhileFiltered++;

        Datastore.AddDataflow();
    }

    private async void OnDataflowDeleteStartedAsync(DataflowStructureTreeNode node)
    {
        _currentDeletingNode = node;

        var dataflowRoot = _currentDeletingNode.Dataflow.Root;
        var dataflowFilled = dataflowRoot.Containers.Count > 0
            || dataflowRoot.FunctionBlocks.Count > 0
            || dataflowRoot.Labels.Count > 0
            || _currentDeletingNode.Dataflow.DataPorts.Count > 0;

        if (dataflowFilled)
        {
            _currentDeletingNode.Deleting = true;
            _treeBuilder.Notifications.NotifyNodeChanged(_currentDeletingNode);

            await _confirmDeleteDialogRef!.OpenAsync();
        }
        else
        {
            Datastore.RemoveDataflow(_currentDeletingNode.Dataflow);
        }
    }

    private void OnFilterTextChanged(string newText)
    {
        TreeAdapter.FilterNodes(newText);

        if (!_treeBuilder.Filter.IsFilterActive)
            _dataflowsAddedWhileFiltered = 0;
    }

    protected override void OnInitialized()
    {
        Datastore.BuilderChanged += OnBuilderChangedAsync;
        DiagramEventService.ContainerLoaded += OnContainerLoadedAsync;
        TreeAdapter.DeleteStarted += OnDataflowDeleteStartedAsync;

        _treeBuilder.SetAdapter(TreeAdapter);
        _treeBuilder.Selection.SelectionChanged += OnSelectionChanged;

        // If the component gets initialized after cluster was loaded it gets
        // initialized after builder changed if used within expandle button
        if (!Datastore.HasBuilder)
            return;

        TryInitTreeAdapter();
    }

    private void OnSelectionChanged(ITreeNode treeNode, bool selected)
        => _currentSelectedNode = treeNode;

    [LoggerMessage(1, LogLevel.Error, "Failed to reassign builder with cluster id:{ClusterId} v{Version}", EventName = "ReassignBuilderFailed")]
    public static partial void ReassignBuilderFailed(ILogger logger, Exception ex, Guid ClusterId, Version Version);

    private void TryInitTreeAdapter()
    {
        try
        {
            TreeAdapter.UseBuilder(Datastore.Builder);
        }
        catch (Exception ex)
        {
            ReassignBuilderFailed(Logger, ex, Datastore.Builder.Cluster.Id, Datastore.Builder.Cluster.Version);
        }
    }
}
