using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.Localization;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class DataPortEditingCoordinator(IClusterEditorManagementInternal dataManagementService, DataPortTreeState state)
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
        }

        if (dpNode.Id != editingTreeNode?.Id || !editingTreeNode.IsEditModeActive)
        {
            dpNode.IsEditModeActive = true;
            dpNode.HasChangedProperties = false;
        }

        state.EditingTreeNode = dpNode;
        state.Builder.Notifications.NotifyNodeChanged(dpNode, ChangedNodeDetail.None);
    }

    private void ScrollBackToEditingNode()
    {
        if (state.EditingTreeNode is { } editingTreeNode)
            editingTreeNode.ScrollToNode(state.Builder);
    }
}
