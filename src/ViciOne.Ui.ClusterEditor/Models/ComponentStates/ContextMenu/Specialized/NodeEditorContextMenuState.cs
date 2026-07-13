using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Helpers;
using ViciOne.Ui.ClusterEditor.Models.Comparer;
using ViciOne.Ui.ClusterEditor.Models.ContextMenu;
using ViciOne.Ui.ClusterEditor.Models.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Models.ComponentStates.ContextMenu.Specialized;

public sealed class NodeEditorContextMenuState(SelectionManager selectionManager, IDatastore datastore, DiagramService diagramService) : IContextMenuState<NodeEditorContextMenuContext>
{
    public bool AlignmentButtonsEnabled { get; private set; }
    public bool ConnectorsWizardEnabled { get; private set; }
    public double ContextMenuPositionX { get; private set; }
    public double ContextMenuPositionY { get; private set; }
    public bool DissolveContainerEnabled { get; private set; }
    public bool EditContainerEnabled { get; private set; }
    public IEnumerable<EngineContextMenuEntry> EngineAssignmentContextMenuEntries { get; private set; } = [];
    public bool EngineAssignmentEnabled { get; private set; }
    public bool MoveToNewContainerEnabled { get; private set; }
    public object? ObjectOpenedOn { get; private set; }
    public bool SelectAllConnectorsEnabled { get; private set; }
    public bool SelectAllEnabled { get; private set; }
    public bool SelectInputAndOutputConnectorsEnabled { get; private set; }
    public bool SelectInputConnectorsEnabled { get; private set; }
    public bool SelectInputConnectorsIncludingSystemConnectorsEnabled { get; private set; }
    public bool SelectOutputConnectorsEnabled { get; private set; }
    public bool SelectOutputConnectorsIncludingSystemConnectorsEnabled { get; private set; }
    public bool SettingsEnabled { get; private set; }

    private List<EngineContextMenuEntry> BuildEngineEntries()
    {
        var selectedBlockCount = selectionManager.SelectedFBs.Count + selectionManager.SelectedContainers.Count;

        List<FunctionBlock> selectedFbs = [];
        foreach (var fb in datastore.DataflowDiagramMapping.GetModels(selectionManager.SelectedFBs))
            selectedFbs.Add(fb);

        List<ChildContainer> selectedContainers = [];
        foreach (var c in datastore.DataflowDiagramMapping.GetModels(selectionManager.SelectedContainers))
            selectedContainers.Add(c);

        var menuEntryChildren = new List<EngineContextMenuEntry>();

        foreach (var engine in datastore.ValidDataflowEngines)
        {
            var engineDisplayText = EngineDisplayText.Get(datastore.Builder, engine);
            var blocksWithEngineCount = 0;

            foreach (var fb in selectedFbs)
            {
                if (fb.Engine == engine)
                    blocksWithEngineCount++;
            }

            foreach (var c in selectedContainers)
            {
                foreach (var e in c.GetEngines())
                {
                    if (e == engine)
                    {
                        blocksWithEngineCount++;
                        break;
                    }
                }
            }

            menuEntryChildren.Add(new EngineContextMenuEntry
            {
                Engine = engine,
                IconCssClass = ContextMenuHelper.GetIconUrl(blocksWithEngineCount, selectedBlockCount),
                Text = $"{engineDisplayText} - {engine.Name}"
            });
        }

        menuEntryChildren.Sort((a, b) => AlphaNumericComparer<string>.Default.Compare(a.Text, b.Text));

        if (menuEntryChildren.Count > 0)
            menuEntryChildren[0].BeginGroup = true;

        return menuEntryChildren;
    }

    private bool HasAnyConnector()
    {
        foreach (var bn in selectionManager.SelectedBlockNodes)
        {
            foreach (var row in bn.Connectors)
            {
                foreach (var c in row)
                {
                    if (c is not null)
                        return true;
                }
            }
        }

        return false;
    }

    private static bool HasNestedFunctionBlocks(ChildContainer container)
    {
        foreach (var _ in container.GetAllNestedFunctionBlocks())
            return true;

        return false;
    }

    private bool HasSharedSettings(int fbCount)
    {
        var settingCounts = new Dictionary<(string Name, System.Type SettingType), int>();
        foreach (var setting in selectionManager.SelectedFBs.GetSettings(datastore))
        {
            var key = (setting.Name, setting.SettingType);
            if (settingCounts.TryGetValue(key, out var count))
                settingCounts[key] = count + 1;
            else
                settingCounts[key] = 1;
        }

        foreach (var count in settingCounts.Values)
        {
            if (count == fbCount)
                return true;
        }

        return false;
    }

    private bool IsMoveToNewContainerEnabled()
    {
        var selected = selectionManager.SelectedBlockNodes;
        var children = new IContainerChild[selected.Count];
        for (var i = 0; i < selected.Count; i++)
            children[i] = datastore.DataflowDiagramMapping.GetModel(selected[i]);

        return Cluster.Builder.ContainerEditor.CanAddContainer(children);
    }

    public void Update(NodeEditorContextMenuContext context)
    {
        ObjectOpenedOn = context.ObjectOpenedOn;
        ContextMenuPositionX = context.MouseEventArgs.ClientX;
        ContextMenuPositionY = context.MouseEventArgs.ClientY;

        SelectAllEnabled = false;
        foreach (var node in diagramService.Diagram.Nodes)
        {
            if (node is BlockNode)
            {
                SelectAllEnabled = true;
                break;
            }
        }

        MoveToNewContainerEnabled = IsMoveToNewContainerEnabled();
        AlignmentButtonsEnabled = selectionManager.SelectedBlockNodes.Count + selectionManager.SelectedLabels.Count >= 2;

        if (context.Block is not null)
            UpdateBlockRelatedState(context.Block);
    }

    private void UpdateBlockRelatedState(Block block)
    {
        EngineAssignmentContextMenuEntries = BuildEngineEntries();

        UpdateConnectorSelectionStates();

        if (block.IsChildContainer)
        {
            DissolveContainerEnabled =
                selectionManager.SelectedContainers.Count == 1 && selectionManager.SelectedModels.Count == 1;

            EditContainerEnabled =
                selectionManager.SelectedContainers.Count == 1
                    && selectionManager.SelectedModels.Count == 1
                    && selectionManager.SelectedContainers[0].ConnectorsToList().Count > 0;
        }

        EngineAssignmentEnabled = block.IsFunctionBlock || HasNestedFunctionBlocks(block.ChildContainer!);

        var fbCount = selectionManager.SelectedFBs.Count;
        SettingsEnabled = HasSharedSettings(fbCount);

        ConnectorsWizardEnabled = HasAnyConnector();
    }

    private void UpdateConnectorSelectionStates()
    {
        var hasInput = false;
        var hasOutput = false;
        var hasNonSystemInput = false;
        var hasSystemInput = false;
        var hasNonSystemOutput = false;
        var hasSystemOutput = false;

        foreach (var bn in selectionManager.SelectedBlockNodes)
        {
            foreach (var row in bn.Connectors)
            {
                if (row[0] is { } input)
                {
                    hasInput = true;
                    if (input.IsSystemConnector)
                        hasSystemInput = true;
                    else
                        hasNonSystemInput = true;
                }

                if (row[1] is { } output)
                {
                    hasOutput = true;
                    if (output.IsSystemConnector)
                        hasSystemOutput = true;
                    else
                        hasNonSystemOutput = true;
                }
            }
        }

        SelectAllConnectorsEnabled = hasInput || hasOutput;
        SelectInputConnectorsEnabled = hasNonSystemInput;
        SelectInputConnectorsIncludingSystemConnectorsEnabled = hasInput && hasSystemInput;
        SelectOutputConnectorsEnabled = hasNonSystemOutput;
        SelectOutputConnectorsIncludingSystemConnectorsEnabled = hasOutput && hasSystemOutput;
        SelectInputAndOutputConnectorsEnabled = hasNonSystemInput && hasNonSystemOutput;
    }
}
