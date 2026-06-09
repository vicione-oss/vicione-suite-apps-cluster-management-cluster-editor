using System.Collections.Generic;
using System.Linq;
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
        var selectedContainers = datastore.DataflowDiagramMapping.GetModels(selectionManager.SelectedContainers).ToArray();
        var selectedFbs = datastore.DataflowDiagramMapping.GetModels(selectionManager.SelectedFBs).ToArray();

        var menuEntryChildren = new List<EngineContextMenuEntry>();

        foreach (var engine in datastore.ValidDataflowEngines)
        {
            var engineDisplayText = EngineDisplayText.Get(datastore.Builder, engine);
            var blocksWithEngineCount = selectedFbs.Count(fb => fb.Engine == engine)
                + selectedContainers.Count(c => c.GetEngines().Contains(engine));

            var entry = new EngineContextMenuEntry()
            {
                Engine = engine,
                IconCssClass = ContextMenuHelper.GetIconUrl(blocksWithEngineCount, selectedFbs.Length + selectedContainers.Length),
                Text = $"{engineDisplayText} - {engine.Name}"
            };

            menuEntryChildren.Add(entry);
        }

        var orderedEngines = menuEntryChildren.OrderBy(e => e.Text, AlphaNumericComparer<string>.Default);
        var firstEntry = orderedEngines.FirstOrDefault();
        firstEntry?.BeginGroup = true;

        return [.. orderedEngines];
    }

    private bool IsMoveToNewContainerEnabled()
        => Cluster.Builder.ContainerEditor.CanAddContainer(
            [.. selectionManager.SelectedBlockNodes.Select(n => (IContainerChild)datastore.DataflowDiagramMapping.GetModel(n))]);

    public void Update(NodeEditorContextMenuContext context)
    {
        ObjectOpenedOn = context.ObjectOpenedOn;
        ContextMenuPositionX = context.MouseEventArgs.ClientX;
        ContextMenuPositionY = context.MouseEventArgs.ClientY;
        SelectAllEnabled = diagramService.Diagram.Nodes.Any(x => x is BlockNode);
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
                    && selectionManager.SelectedContainers[0].ConnectorsToList().Any();
        }

        EngineAssignmentEnabled =
            block.IsFunctionBlock || block.ChildContainer!.GetAllNestedFunctionBlocks().Any();

        var fbCount = selectionManager.SelectedFBs.Count;

        SettingsEnabled = selectionManager.SelectedFBs
            .GetSettings(datastore)
            .ToArray()
            .GroupBy(s => new { s.Name, s.SettingType })
            .Any(sg => sg.Count() == fbCount);

        ConnectorsWizardEnabled = selectionManager.SelectedBlockNodes.Any(bn => bn.Connectors.Any(cons => cons.Any(c => c is not null)));
    }

    private void UpdateConnectorSelectionStates()
    {
        var selectedBlockNodeConnectors = selectionManager.SelectedBlockNodes.SelectMany(bn => bn.Connectors).ToArray();
        var inputConnectors = selectedBlockNodeConnectors.Select(row => row[0]).Where(bnc => bnc is not null).ToArray();
        var outputConnectors = selectedBlockNodeConnectors.Select(row => row[1]).Where(bnc => bnc is not null).ToArray();

        SelectAllConnectorsEnabled = inputConnectors.Length != 0 || outputConnectors.Length != 0;
        SelectInputConnectorsEnabled = inputConnectors.Any(ic => !ic!.IsSystemConnector);
        SelectInputConnectorsIncludingSystemConnectorsEnabled = inputConnectors.Length != 0 && inputConnectors.Any(ic => ic!.IsSystemConnector);
        SelectOutputConnectorsEnabled = outputConnectors.Any(oc => !oc!.IsSystemConnector);
        SelectOutputConnectorsIncludingSystemConnectorsEnabled = outputConnectors.Length != 0 && outputConnectors.Any(oc => oc!.IsSystemConnector);
        SelectInputAndOutputConnectorsEnabled = inputConnectors.Any(ic => !ic!.IsSystemConnector) && outputConnectors.Any(oc => !oc!.IsSystemConnector);
    }
}
