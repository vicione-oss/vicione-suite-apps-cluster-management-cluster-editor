using System;
using System.Linq;
using System.Threading.Tasks;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Components.ToolbarDataflow.Models;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components.NodeEditorServices;

internal sealed class DiagramModelSyncController(
    IDatastore datastore,
    DiagramEventService diagramEventService,
    DiagramService diagramService,
    IPropertyGridController<DataflowToolbarPropertyGridContext> propertyGridController,
    SelectionManager selectionManager) : IDisposable
{
    private BlazorDiagram? _diagram;
    private bool _initialized;

    public event Func<LabelNode, Task>? LabelEditModeStarted;

    public void Dispose()
        => Teardown();

    public void Initialize(BlazorDiagram diagram)
    {
        if (_initialized)
            Teardown();

        _diagram = diagram;

        _diagram.Links.Added += OnDiagramLinksAdded;
        _diagram.Links.Removed += OnDiagramLinksRemoved;
        _diagram.Nodes.Added += OnDiagramNodesAdded;
        _diagram.Nodes.Removed += OnDiagramNodesRemoved;
        _diagram.ZoomChanged += OnDiagramZoomChanged;

        diagramEventService.ZoomChanged += OnDiagramStateZoomChanged;

        selectionManager.DiagramSelectionChanged += OnDiagramSelectionChanged;

        _initialized = true;
    }

    private void OnDiagramLinksAdded(BaseLinkModel link)
    {
        if (link is not BlockNodeLink fbNodeLink)
            return;

        fbNodeLink.TargetAttached += OnLinkTargetAttached;

        // Wenn ein Link nicht "attached" ist bedeutet das, dass dieser vom Diagramm
        // generiert wurde und gerade vom Benutzer gezogen wird.
        // Wenn ein Link bereits "attached" ist bedeutet das, dass dieser über den
        // Code hinzugefügt wurde.
        if (!link.IsAttached)
            diagramService.SetDraggingLink(fbNodeLink);
    }

    private void OnDiagramLinksRemoved(BaseLinkModel link)
    {
        if (diagramService.DiagramState.SuppressEvents)
            return;

        if (link is not BlockNodeLink fbNodeLink)
            return;

        fbNodeLink.TargetAttached -= OnLinkTargetAttached;

        // Links, die nicht "attached" sind, wurden nie dem Datastore hinzugefügt. Höchstwahrscheinlich
        // sind es Links die von Benutzer gezogen wurden oder die generiert wurden, wenn man einen Port
        // selektiert hat.
        if (fbNodeLink.IsAttached)
        {
            if (diagramService.DiagramState.SimplifiedView)
            {
                fbNodeLink.SourceNode.Refresh();
                fbNodeLink.TargetNode?.Refresh();
            }

            datastore.Remove(fbNodeLink);
        }
        else
        {
            diagramService.SetDraggingLink(null);
        }
    }

    private void OnDiagramNodesAdded(NodeModel node)
    {
        if (node is LabelNode labelNode)
        {
            labelNode.EditModeStarted += OnLabelNodeEditModeStarted;
            labelNode.OrderChanged += OnLabelNodeOrderChanged;

            if (!diagramService.DiagramState.SuppressEvents)
            {
                diagramService.Diagram.SendToBack(labelNode);
            }
        }
    }

    private void OnDiagramNodesRemoved(NodeModel node)
    {
        if (diagramService.DiagramState.SuppressEvents)
            return;

        if (node is LabelNode labelNode)
        {
            labelNode.EditModeStarted -= OnLabelNodeEditModeStarted;
            labelNode.OrderChanged -= OnLabelNodeOrderChanged;
            datastore.Remove(labelNode);
        }
        else if (node is FunctionBlockNode functionBlockNode)
        {
            datastore.Remove(functionBlockNode);
        }
        else if (node is ChildContainerNode containerNode)
        {
            datastore.Remove(containerNode);
        }

        UpdatePropertyGrid();
    }

    private Task OnDiagramSelectionChanged(SelectableModel model)
    {
        if (diagramService.DiagramState.SuppressEvents)
            return Task.CompletedTask;

        if (diagramService.DiagramState.NewlyCreatedLabel is not null &&
            model == diagramService.DiagramState.NewlyCreatedLabel &&
            !diagramService.DiagramState.NewlyCreatedLabel.Selected)
        {
            diagramService.DiagramState.NewlyCreatedLabel.Locked = diagramService.DiagramState.LabelsLocked;
            diagramService.DiagramState.NewlyCreatedLabel = null;
        }

        // Da ein Connector (Port) und dessen Published Connector Marker kein SelectableModel sind
        // und somit deren Selektion nicht vom Diagramm verwaltet werden, können wir alle Connectors
        // und deren Published Connector Marker deselektieren wenn sich hier die Selektion ändert.
        if (model is not null)
            selectionManager.DeselectAll(SelectionMode.FunctionBlockConnector | SelectionMode.ConnectorMarker);

        UpdatePropertyGrid();

        return Task.CompletedTask;
    }

    private void OnDiagramStateZoomChanged(double newZoom)
    {
        if (_diagram is null || _diagram.Container is null)
            return;

        // Berechnung übernommen & angepasst aus GimpZoomBehavior.OnWheel().
        // TODO: Prüfen ob Zoomberechnung ausgelagert und vereinheitlicht werden kann
        if (!_diagram.Options.Zoom.Enabled || (newZoom is < DiagramSettings.ZoomMinimum or > DiagramSettings.ZoomMaximum))
            return;

        var oldZoom = _diagram.Zoom;
        newZoom = Math.Clamp(newZoom, _diagram.Options.Zoom.Minimum, _diagram.Options.Zoom.Maximum);
        if (newZoom == _diagram.Zoom)
            return;

        var clientWidth = _diagram.Container.Width;
        var clientHeight = _diagram.Container.Height;
        var widthDiff = (clientWidth * newZoom) - (clientWidth * oldZoom);
        var heightDiff = (clientHeight * newZoom) - (clientHeight * oldZoom);

        var viewportRect = _diagram.GetViewport();
        var diagramCenterScreenPoint = _diagram.GetScreenPoint(
            viewportRect.Left + (viewportRect.Width / 2),
            viewportRect.Top + (viewportRect.Height / 2)
        );

        var clientX = diagramCenterScreenPoint.X - _diagram.Container.Left;
        var clientY = diagramCenterScreenPoint.Y - _diagram.Container.Top;
        var xFactor = (clientX - _diagram.Pan.X) / oldZoom / clientWidth;
        var yFactor = (clientY - _diagram.Pan.Y) / oldZoom / clientHeight;

        _diagram.Batch(() =>
        {
            _diagram.UpdatePan(-(widthDiff * xFactor), -(heightDiff * yFactor));
            _diagram.SetZoom(newZoom);
        });
    }

    private void OnDiagramZoomChanged()
        => diagramService.SetZoom(_diagram!.Zoom);

    private Task OnLabelNodeEditModeStarted(LabelNode labelNode)
        => LabelEditModeStarted.InvokeEventAsync(labelNode);

    private void OnLabelNodeOrderChanged(SelectableModel model)
    {
        if (diagramService.DiagramState.SuppressEvents)
            return;

        datastore.Builder.Editors.Label.SetZIndex(datastore.DataflowDiagramMapping.GetModel((model as LabelNode)!), model.Order);
    }

    private void OnLinkTargetAttached(BaseLinkModel link)
    {
        if (link is not BlockNodeLink nodeLink)
            return;

        if (datastore.AddLink(nodeLink))
        {
            nodeLink.DrawOverlay = true;
            nodeLink.Refresh();
        }
        else
        {
            diagramService.DiagramState.SuppressEvents = true;
            _diagram!.Links.Remove(nodeLink);
            diagramService.DiagramState.SuppressEvents = false;
        }
    }

    private void Teardown()
    {
        if (_diagram is not null)
        {
            _diagram.Links.Added -= OnDiagramLinksAdded;
            _diagram.Links.Removed -= OnDiagramLinksRemoved;
            _diagram.Nodes.Added -= OnDiagramNodesAdded;
            _diagram.Nodes.Removed -= OnDiagramNodesRemoved;
            _diagram.ZoomChanged -= OnDiagramZoomChanged;
        }

        diagramEventService.ZoomChanged -= OnDiagramStateZoomChanged;
        selectionManager.DiagramSelectionChanged -= OnDiagramSelectionChanged;

        _diagram = null;
        _initialized = false;
    }

    private void UpdatePropertyGrid()
    {
        var selectedContainers = datastore.DataflowDiagramMapping.GetModels(selectionManager.SelectedContainers);
        var selectedFunctionBlocks = datastore.DataflowDiagramMapping.GetModels(selectionManager.SelectedFBs);
        var selectedLinks = datastore.DataflowDiagramMapping.GetModels(selectionManager.SelectedLinks);
        var selectedLabels = datastore.DataflowDiagramMapping.GetModels(selectionManager.SelectedLabels);

        var instances = Array.Empty<object>()
            .Concat(selectedContainers)
            .Concat(selectedLabels)
            .Concat(selectedFunctionBlocks)
            .Concat(selectedLinks);

        propertyGridController.SetInstances(instances, new DataflowToolbarPropertyGridContext());
    }
}
