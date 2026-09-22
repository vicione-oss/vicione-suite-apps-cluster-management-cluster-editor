using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Components.Localization;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.Localization;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class DataPortEditingCoordinator(
    IClusterEditorManagementInternal dataManagementService,
    DataPortTreeMutator mutator,
    DataPortChildNodePropertyValueStore propertyValueStore,
    DataPortTreeState state)
{
    public async Task BeginEdit(DataPortNodeModel dpNode)
    {
        var editingTreeNode = state.EditingTreeNode;

        if (editingTreeNode is not null && editingTreeNode.IsEditModeActive)
        {
            if (editingTreeNode.HasChangedProperties)
            {
                // Resolved when the toast is closed rather than captured here: by then the node may
                // have gone with a deleted ancestor, and the tree builder would throw for it.
                await dataManagementService.ShowMessageToast(LogLevel.Warning, SharedSectionText.UnsavedNodeChanges, ScrollBackToEditingNode);
                return;
            }

            editingTreeNode.IsEditModeActive = false;
            state.Builder.Notifications.NotifyNodeChanged(editingTreeNode, ChangedNodeDetail.None);
            editingTreeNode.NotifyDescendantsChanged(state.Builder);
        }

        if (dpNode.Id != editingTreeNode?.Id || !editingTreeNode.IsEditModeActive)
        {
            dpNode.IsEditModeActive = true;
            dpNode.HasChangedProperties = false;
            state.PendingEditValues = ReadOnlyDictionary<string, object?>.Empty;
        }

        state.EditingTreeNode = dpNode;
        state.Builder.Notifications.NotifyNodeChanged(dpNode, ChangedNodeDetail.None);
        dpNode.NotifyDescendantsChanged(state.Builder);
    }

    /// <summary>
    /// The open edit form with the values the user changed in it, or <see langword="null"/> when no form is open.
    /// </summary>
    public DataPortPendingEdit? CapturePendingEdit()
    {
        if (state.EditingTreeNode is not DataPortChildNodeModel { IsEditModeActive: true } editingTreeNode)
            return null;

        var changedValues = editingTreeNode.HasChangedProperties
            ? GetChangedValues(editingTreeNode)
            : ReadOnlyDictionary<string, object?>.Empty;

        return new DataPortPendingEdit(editingTreeNode.Id, changedValues);
    }

    private IReadOnlyDictionary<string, object?> GetChangedValues(DataPortChildNodeModel node)
    {
        var changedValues = new Dictionary<string, object?>();

        foreach (var (propertyName, value) in propertyValueStore.Values)
        {
            if (TryGetModelValue(node, propertyName, out var modelValue) && !Equals(value, modelValue))
                changedValues[propertyName] = value;
        }

        return changedValues;
    }

    /// <summary>
    /// The values the edit form of <paramref name="node"/> shows in place of the node's own values.
    /// </summary>
    public IReadOnlyDictionary<string, object?> GetPendingValues(DataPortChildNodeModel node)
        => ReferenceEquals(state.EditingTreeNode, node) ? state.PendingEditValues : ReadOnlyDictionary<string, object?>.Empty;

    /// <summary>
    /// Opens the edit form of <paramref name="pendingEdit"/> again on the node with the same id, filled with the
    /// changed values. A warning is shown when the node no longer exists and changed values are lost.
    /// </summary>
    public async Task RestorePendingEditAsync(DataPortPendingEdit? pendingEdit)
    {
        if (pendingEdit is null)
            return;

        if (state.FindAnyNode(pendingEdit.NodeId) is not DataPortChildNodeModel node)
        {
            if (pendingEdit.ChangedValues.Count != 0)
                await dataManagementService.ShowMessageToast(LogLevel.Warning, DataPortSection.PendingChangesDiscarded, () => { });

            return;
        }

        node.IsEditModeActive = true;
        node.HasChangedProperties = pendingEdit.ChangedValues.Count != 0;
        state.EditingTreeNode = node;
        state.PendingEditValues = pendingEdit.ChangedValues;

        state.Builder.Notifications.NotifyNodeChanged(node, ChangedNodeDetail.None);
        node.NotifyDescendantsChanged(state.Builder);
        node.ScrollToNode(state.Builder);
    }

    private void ScrollBackToEditingNode()
    {
        if (state.EditingTreeNode is { } editingTreeNode)
            editingTreeNode.ScrollToNode(state.Builder);
    }

    private void ScrollToNodeIfStillInTree(GuidNodeIdentifier nodeId)
    {
        if (state.FindAnyNode(nodeId) is { } treeNode)
            treeNode.ScrollToNode(state.Builder);
    }

    public async Task<bool> TryConfirmEditAsync(DataPortChildNodeModel childNode, DataPortChildNodePropertyValueStore pendingValues)
    {
        if (!mutator.CanApplyNodeChanges(childNode, pendingValues, out var errorMessage))
        {
            var nodeId = childNode.Id;
            await dataManagementService.ShowMessageToast(LogLevel.Warning, errorMessage, () => ScrollToNodeIfStillInTree(nodeId));
            return false;
        }

        // The success path must not await anything, so the cluster cannot change between validation and commit.
        childNode.AssignValuesAndProperties(pendingValues);
        return true;
    }

    private static bool TryGetModelValue(DataPortChildNodeModel node, string propertyName, out object? value)
    {
        switch (propertyName)
        {
            case nameof(DataPortChildNodeModel.Name):
                value = node.Name;
                return true;

            case nameof(DataPortChildNodeModel.Icon):
                value = node.Icon;
                return true;
        }

        var property = node.Properties.FirstOrDefault(p => p.Name == propertyName);
        value = property?.Value;
        return property is not null;
    }
}
