using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.Localization;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

internal sealed class DataPortEditingCoordinator(IClusterEditorManagementInternal dataManagementService, DataPortTreeState state)
{
    public async Task BeginEdit(DataPortNodeModel dpNode)
    {
        var editingTreeNode = state.EditingTreeNode;

        if (editingTreeNode is not null && editingTreeNode.IsEditModeActive)
        {
            if (editingTreeNode.HasChangedProperties)
            {
                await dataManagementService.ShowMessageToast(LogLevel.Warning, SharedSectionText.UnsavedNodeChanges, () => editingTreeNode.ScrollToNode(state.Builder));
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
}
