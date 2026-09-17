using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.Localization;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class DataPortEditingCoordinator(IClusterEditorManagementInternal dataManagementService, DataPortTreeMutator mutator, DataPortTreeState state)
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
        }

        state.EditingTreeNode = dpNode;
        state.Builder.Notifications.NotifyNodeChanged(dpNode, ChangedNodeDetail.None);
        dpNode.NotifyDescendantsChanged(state.Builder);
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
}
