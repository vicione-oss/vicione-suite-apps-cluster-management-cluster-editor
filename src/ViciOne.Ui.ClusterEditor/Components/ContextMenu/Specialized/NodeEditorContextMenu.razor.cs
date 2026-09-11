using System.Linq;
using System.Threading.Tasks;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models.Base;
using Microsoft.AspNetCore.Components;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.Blazor.Components.ContextMenu.Components;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor;
using ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Localization;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.ComponentStates.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Models.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using TechnicalTerms = ViciOne.Ui.ClusterEditor.Localization.Resources.TechnicalTerms;

namespace ViciOne.Ui.ClusterEditor.Components.ContextMenu.Specialized;

public sealed partial class NodeEditorContextMenu : SpecializedContextMenuWithStateBase<NodeEditorContextMenuContext, NodeEditorContextMenuState>
{
    private readonly string _selectAllConnectorsText = CompositeFormats.SelectSomething(TechnicalTerms.ConnectorPlural);
    private readonly string _selectInputConnectorsText = CompositeFormats.SelectSomething(TechnicalTerms.InputConnectorPlural);
    private readonly string _selectOutputConnectorsText = CompositeFormats.SelectSomething(TechnicalTerms.OutputConnectorPlural);
    private bool _shouldFocusDiagram = true;

    [Inject] private ClusterBuilderEventBuffer ClusterBuilderEventBuffer { get; set; } = default!;
    [Inject] private ConnectorSelectionDialogService ConnectorSelectionDialogService { get; set; } = default!;
    [Inject] private IContainerEditorRequest ContainerEditorRequest { get; set; } = default!;
    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;
    [Inject] private DiagramService DiagramService { get; set; } = default!;
    [Inject] private IFbSettingsEditorRequest FbSettingsEditorRequest { get; set; } = default!;
    [Inject] private LabelOrderService LabelOrderService { get; set; } = default!;
    [Inject] private SelectionManager SelectionManager { get; set; } = default!;

    private async Task AddContainerClickAsync()
    {
        var position = DiagramService.Diagram.GetRelativeGridPoint(new Point(State.ContextMenuPositionX, State.ContextMenuPositionY), Datastore);
        var containerNode = await Datastore.AddChildContainer(DiagramService, position);

        DiagramService.Diagram.Nodes.Add(containerNode);
        SelectionManager.SetSelection(containerNode);
    }

    private void AddLabelClick()
    {
        var zIndex = GetNextLabelZIndex();
        var position = DiagramService.Diagram.GetRelativeGridPoint(new Point(State.ContextMenuPositionX, State.ContextMenuPositionY), Datastore);
        var labelNode = Datastore.AddLabel(position, zIndex);
        DiagramService.DiagramState.NewlyCreatedLabel = labelNode;

        DiagramService.Diagram.Nodes.Add(labelNode);
        SelectionManager.SetSelection(labelNode);
    }

    private void AlignmentButtonClicked(Alignment alignment)
        => alignment.ApplyToSelection(SelectionManager);

    private async Task CloseContextMenuRequested()
    {
        if (ContextMenu is not null)
            await ContextMenu.CloseAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        DiagramEventService.CloseContextMenuRequested -= CloseContextMenuRequested;
    }

    private async Task DissolveContainerClickAsync()
    {
        ClusterBuilderEventBuffer.StartBatchOpertation();

        await Datastore.DissolveContainerAsync(
            Datastore.DataflowDiagramMapping.GetModel(SelectionManager.SelectedContainers[0]),
            DiagramService,
            SelectionManager
        );

        ClusterBuilderEventBuffer.EndBatchOperation();
    }

    private async Task EditContainerClickAsync()
    {
        _shouldFocusDiagram = false;

        await ContainerEditorRequest.SendAsync();
    }

    private int GetHighestZIndex()
        => Datastore.DataflowDiagramMapping
            .GetLabelModels()
            .Aggregate(-10001, (max, cur) => cur.ZIndex > max ? cur.ZIndex : max);

    private string GetLockLabelsText()
    {
        if (DiagramService.DiagramState.LabelsLocked)
            return Localization.NodeEditorContextMenu.UnlockLabels;
        else
            return Localization.NodeEditorContextMenu.LockLabels;
    }

    private int GetNextLabelZIndex()
        => GetHighestZIndex() + 1;

    private void LabelBringToFrontClick()
        => LabelOrderService.BringToFront(SelectionManager.SelectedLabels);

    private void LabelSendToBackClick()
        => LabelOrderService.SendToBack(SelectionManager.SelectedLabels);

    private void LockLabelsClick()
    {
        DiagramService.DiagramState.LabelsLocked ^= true;

        var labelNodes = DiagramService.Diagram.Nodes.OfType<LabelNode>().ToArray();
        if (labelNodes.Length == 0)
            return;

        var selectedLabelNodes = SelectionManager.SelectedLabels;
        foreach (var node in selectedLabelNodes)
            DiagramService.Diagram.UnselectModel(node);

        foreach (var node in labelNodes)
        {
            node.Locked = DiagramService.DiagramState.LabelsLocked;
            node.Refresh();
        }
    }

    private async Task MoveToNewContainerClickAsync()
    {
        var selectedFbs = Datastore.DataflowDiagramMapping.GetModels(SelectionManager.SelectedFBs).ToArray();
        var selectedContainers = Datastore.DataflowDiagramMapping.GetModels(SelectionManager.SelectedContainers).ToArray();
        var selectedLabels = Datastore.DataflowDiagramMapping.GetModels(SelectionManager.SelectedLabels).ToArray();

        Point newContainerLocation = new(0, 0);
        if (State.ObjectOpenedOn is MovableModel node)
            newContainerLocation = node.Position;

        ClusterBuilderEventBuffer.StartBatchOpertation();
        await Datastore.MoveToNewContainerAsync(DiagramService, newContainerLocation, selectedFbs, selectedContainers, selectedLabels, SelectionManager);
        ClusterBuilderEventBuffer.EndBatchOperation();
    }

    private void OnConnectorsSelectionWizardClicked()
    {
        _shouldFocusDiagram = false;

        var sourceConnectors = SelectionManager.SelectedBlockNodes
            .SelectMany(bn => bn.ConnectorsToList())
            .Select(Datastore.DataflowDiagramMapping.GetModel);

        ConnectorSelectionDialogService.SetSourceConnectors(sourceConnectors);
        ConnectorSelectionDialogService.SetVisibility(true);
    }

    private void OnContextMenuVisibilityChanged(bool isVisible)
    {
        DiagramService.DiagramState.IsContextMenuVisible = isVisible;

        if (isVisible)
        {
            _shouldFocusDiagram = true;

            DiagramEventService.CloseContextMenuRequested += CloseContextMenuRequested;
        }
        else
        {
            DiagramEventService.CloseContextMenuRequested -= CloseContextMenuRequested;

            // The context menu closes asynchronously, so this runs after a menu item has already opened
            // its dialog and has focused itself. Without the flag the diagram would take the focus
            // straight back and keyboard input would act on the selection behind the dialog.
            if (_shouldFocusDiagram)
                DiagramEventService.RequestDiagramFocus();
        }
    }

    private void OnDeleteClicked()
    {
        ClusterBuilderEventBuffer.StartBatchOpertation();

        OnRemoveSelectedBlocksRequested();
        OnRemoveSelectedContainersRequested();
        OnRemoveSelectedLabelsRequested();

        ClusterBuilderEventBuffer.EndBatchOperation();
    }

    private void OnRemoveSelectedBlocksRequested()
        => DiagramService.Diagram.Nodes.Remove(SelectionManager.SelectedFBs);

    private void OnRemoveSelectedContainersRequested()
        => DiagramService.Diagram.Nodes.Remove(SelectionManager.SelectedContainers);

    private void OnRemoveSelectedLabelsRequested()
        => DiagramService.Diagram.Nodes.Remove(SelectionManager.SelectedLabels);

    private void SelectAllBlocksClick()
    {
        SelectionManager.SelectAll(SelectionMode.Container | SelectionMode.FunctionBlock);
        DiagramEventService.RequestDiagramFocus();
    }

    private void SelectAllConnectorsClick()
        => SelectConnectors(true, true, true);

    private void SelectConnectors(bool includeInputConnectors, bool includeOutputConnectors, bool withSystemConnectors)
    {
        var selectedNodes = SelectionManager.SelectedFBs.Select(sfb => (BlockNode)sfb).Concat(SelectionManager.SelectedContainers);
        SelectionManager.DeselectAll();

        foreach (var selectedNode in selectedNodes)
        {
            foreach (var connector in selectedNode.ConnectorsToList())
            {
                if ((includeInputConnectors && connector.IsInput) ||
                    (includeOutputConnectors && !connector.IsInput))
                {
                    if (!connector.IsSystemConnector || withSystemConnectors)
                        SelectionManager.Select(connector);
                }
            }
        }
    }

    private void SelectInputAndOutputConnectorsClick()
        => SelectConnectors(true, true, false);

    private void SelectInputConnectorsClick()
        => SelectConnectors(true, false, false);

    private void SelectInputIncludingSystemConnectorsClick()
        => SelectConnectors(true, false, true);

    private void SelectOutputConnectorsClick()
        => SelectConnectors(false, true, false);

    private void SelectOutputIncludingSystemConnectorsClick()
        => SelectConnectors(false, true, true);

    private async Task SettingsClickAsync()
    {
        _shouldFocusDiagram = false;

        await FbSettingsEditorRequest.SendAsync();
    }

    private void ToggleEngineAssignment(Cluster.Model.Engine engine)
    {
        var selectedContainers = Datastore.DataflowDiagramMapping.GetModels(SelectionManager.SelectedContainers).ToArray();
        var selectedFbs = Datastore.DataflowDiagramMapping.GetModels(SelectionManager.SelectedFBs).ToArray();
        var containerEditor = Datastore.Builder.Editors.Container;
        var functionBlockEditor = Datastore.Builder.Editors.FunctionBlock;

        var assignEngine = selectedContainers.Any(sc => { var engines = sc.GetEngines(); return !engines.Any() || engines.Any(e => !e.Equals(engine)); })
            || selectedFbs.Any(n => n.Engine != engine);

        if (assignEngine)
        {
            containerEditor.AssignEngine(engine, selectedContainers);
            functionBlockEditor.AssignEngine(engine, selectedFbs);
        }
        else
        {
            containerEditor.UnassignEngine(selectedContainers);
            functionBlockEditor.UnassignEngine(selectedFbs);
        }
    }

    private void UnassignEngineClick()
    {
        var selectedContainers = Datastore.DataflowDiagramMapping.GetModels(SelectionManager.SelectedContainers).ToArray();
        var selectedFbs = Datastore.DataflowDiagramMapping.GetModels(SelectionManager.SelectedFBs).ToArray();
        var containerEditor = Datastore.Builder.Editors.Container;
        var functionBlockEditor = Datastore.Builder.Editors.FunctionBlock;

        containerEditor.UnassignEngine(selectedContainers);
        functionBlockEditor.UnassignEngine(selectedFbs);
    }
}
