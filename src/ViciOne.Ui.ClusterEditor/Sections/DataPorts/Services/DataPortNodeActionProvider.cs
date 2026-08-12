using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.ClusterEditor.Localization;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Components.Localization;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.ContextMenu;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions.Arguments;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;
using TechnicalTerms = ViciOne.Ui.ClusterEditor.Localization.Resources.TechnicalTerms;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

internal sealed class DataPortNodeActionProvider(
    IContextMenuRequest<DataPortAddChildNodeContextMenuContext> addChildNodeContextMenuRequest,
    DataPortTreeMutator mutator,
    DataPortEditingCoordinator editingCoordinator,
    DataPortTreeState state)
{
    private static readonly CompositeFormat s_compositeNodeActionAdd = CompositeFormat.Parse(DataPortSection.NodeActionAdd);

    private async void AddNewNodeAsync(NodeButton _, VisibleActionArguments e)
    {
        if (e.Node is not DataPortNodeModel dpNode)
            return;

        if (dpNode.PossibleChildren.Count == 1)
        {
            var childNode = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(dpNode.PossibleChildren[0], dpNode);
            mutator.CreateNewChildNode(dpNode, childNode);
            state.Builder.Expansion.ChangeExpansion(dpNode, true);
            return;
        }

        await addChildNodeContextMenuRequest.SendAsync(new DataPortAddChildNodeContextMenuContext
        {
            MouseEventArgs = e.MouseArgs!,
            ParentNode = dpNode,
            PossibleChildren = dpNode.PossibleChildren
        });
    }

    private async void EditNode(NodeButton _, VisibleActionArguments e)
    {
        if (e.Node is not DataPortNodeModel dpNode)
            return;

        await editingCoordinator.BeginEdit(dpNode);
    }

    public IEnumerable<INodeAction> GetActions(ITreeNode node)
    {
        if (node is not DataPortNodeModel dataPortNode)
            return [];

        var result = new List<INodeAction>();

        var canHaveAdditionalChildren = dataPortNode.PossibleChildren.Any();

        result.Add(new NodeButton()
        {
            Action = AddNewNodeAsync,
            Description = GetNodeActionAddDescription(dataPortNode),
            EnabledFunc = (_) => canHaveAdditionalChildren,
            Icon = new TreeEditorMonochromeIcon(MonochromeIconName.PlusSlim, MonochromeIconSize.Small),
        });

        if (dataPortNode is DataPortChildNodeModel childNode && childNode.Properties.Count > 0)
        {
            result.Add(new NodeButton()
            {
                Action = EditNode,
                Description = CompositeFormats.EditSomething(TechnicalTerms.Node),
                EnabledFunc = (_) => !childNode.IsEditModeActive,
                Icon = new TreeEditorMonochromeIcon(MonochromeIconName.Edit, MonochromeIconSize.Small),
            });
        }

        if (dataPortNode.CanHaveChildren)
        {
            result.Add(new NodeButton()
            {
                Action = SortChildNodes,
                Description = DataPortSection.NodeActionSortChildren,
                EnabledFunc = (node) => dataPortNode.Children.Count > 1,
                Icon = new TreeEditorMonochromeIcon(MonochromeIconName.SortChildren, MonochromeIconSize.Small),
            });
        }

        result.Add(new NodeButton()
        {
            Action = mutator.DeleteDataPortTreeNode,
            Description = CompositeFormats.DeleteSomething(TechnicalTerms.Node),
            EnabledFunc = (_) => true,
            Icon = new TreeEditorMonochromeIcon(MonochromeIconName.Delete, MonochromeIconSize.Small),
        });

        return result;
    }

    private static string GetNodeActionAddDescription(DataPortNodeModel dataPortNode)
        => dataPortNode.PossibleChildren.Count == 1
            ? string.Format(CultureInfo.InvariantCulture, s_compositeNodeActionAdd, dataPortNode.PossibleChildren[0].Name)
            : string.Format(CultureInfo.InvariantCulture, s_compositeNodeActionAdd, DataPortSection.NewChild);

    private void SortChildNodes(NodeButton _, VisibleActionArguments e)
    {
        if (e.Node is not DataPortNodeModel dpNode)
            return;

        if (dpNode.Children.Count == 0)
            return;

        DataPortNodeSorter.SortChildren(dpNode, true);
        state.Builder.Notifications.NotifyChildrenChanged(dpNode);
    }
}
