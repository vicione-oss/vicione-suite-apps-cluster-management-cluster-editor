using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Behaviors;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Behaviors;
using ViciOne.Ui.ClusterEditor.Components.DiagramComponents;
using ViciOne.Ui.ClusterEditor.Components.ToolbarDataflow.Models;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Sections.Library.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components;

[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling", Justification = "#1232")]
public sealed partial class NodeEditor : ComponentBase, IDisposable
{
    private const int LeftMouseButton = 1;
    private const int OrderTopModifier = 100000000;

    private IPanBehavior? _activePanBehavior;
    private bool _contextMenuAllowed = true;
    private BlazorDiagram? _diagram;
    private Model? _diagramPointerDownModel;
    private bool _diagramPointerMoveFirstMove;
    private readonly List<FunctionBlockNode> _draggingNodes = [];
    private Rectangle? _draggingRectangle;
    private Point? _draggingStartPoint;
    private Rectangle? _draggingViewRectangle;
    private VODragMovablesBehavior? _dragMovablesBehavior;
    private VODragNewLinkBehavior? _dragNewLinkBehavior;
    private Label? _editingLabel;
    private GimpPanBehavior? _gimpPanBehavior;
    private GimpZoomBehavior? _gimpZoomBehavior;
    private bool _hasPointerDownShiftKey;
    private bool _isNodeAlignmentBorderVisible;
    private VOKeyboardBehavior? _keyboardBehavior;
    private LabelEditor? _labelEditor;
    private bool _libraryDragInProgress;
    private VOSelectionBehavior? _selectionBehavior;
    private VOPanBehavior? _vOPanBehavior;
    private VOZoomBehavior? _vOZoomBehavior;
    private VOZoomToFitBehavior? _zoomToFitBehavior;

    [Inject] private ClusterBuilderEventBuffer ClusterBuilderEventBuffer { get; set; } = default!;
    [Inject] private ConnectorService ConnectorService { get; set; } = default!;
    [Inject] private IContextMenuRequest<NodeEditorContextMenuContext> ContextMenuRequest { get; set; } = default!;
    [Inject] private IContextMenuSettings ContextMenuSettings { get; set; } = default!;
    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;
    [Inject] private DiagramService DiagramService { get; set; } = default!;
    [Inject] private DragService DragService { get; set; } = default!;
    [Inject] private InputEventService InputEventService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ILibraryService LibraryService { get; set; } = default!;
    [Inject] private LinkDestinationDialogService LinkDestinationDialogService { get; set; } = default!;
    [Inject] private IPropertyGridController<DataflowToolbarPropertyGridContext> PropertyGridController { get; set; } = default!;
    [Inject] private SelectionManager SelectionManager { get; set; } = default!;

    private static void CalculateSelectedModelOrder(SelectableModel model)
    {
        if (model is not ChildContainerNode and not FunctionBlockNode)
            return;

        if (model.Selected)
            model.Order += OrderTopModifier;
        else
            model.Order %= OrderTopModifier;
    }

    private async Task ContainerPointerUpAsync()
    {
        DiagramEventService.RequestEdgeDraggingVisibilityChange(false);
        InputEventService.PointerMove -= OnContainerPointerMoveAsync;

        if (_draggingStartPoint is null)
            return;

        if (_draggingRectangle is null)
        {
            _draggingStartPoint = null;
            return;
        }

        if (_hasPointerDownShiftKey)
        {
            SetDiagramViewport(_draggingRectangle);
        }
        else
        {
            var mode = SelectionMode.Container
                | SelectionMode.FunctionBlock
                | SelectionMode.Label
                | SelectionMode.FunctionBlockLink
                | SelectionMode.ConnectorMarker;

            await SelectionManager.SelectInRectangleAsync(mode, _draggingRectangle);
        }

        _draggingRectangle = null;
        _draggingViewRectangle = null;
        _draggingStartPoint = null;

        await InvokeAsync(StateHasChanged);
    }

    private bool ContextMenuAllowed()
       => _contextMenuAllowed && (_dragMovablesBehavior is null || !_dragMovablesBehavior.IsMoving);

    private void DeleteSelectedConnectorMarkerLinks()
    {
        if (!SelectionManager.SelectedConnectorMarker.Any())
            return;

        if (SelectionManager.SelectedConnectorMarker.Count() == 1)
        {
            var selectedMarker = SelectionManager.SelectedConnectorMarker.First();

            if (selectedMarker.Links.Count == 0 && selectedMarker.Connector.Connector.Published)
            {
                Datastore.Builder.Editors.Connector.SetPublished(selectedMarker.Connector.Connector, false);
                SelectionManager.DeselectAll(SelectionMode.ConnectorMarker);
                return;
            }

            if (selectedMarker.Links.Count == 1)
            {
                ConnectorService.DeleteInvisibleLinks(selectedMarker.Links);
                SelectionManager.DeselectAll(SelectionMode.ConnectorMarker);
            }
            else
            {
                LinkDestinationDialogService.IsDeletionMode = true;
                LinkDestinationDialogService.SetSourceConnectorMarker(selectedMarker);
                LinkDestinationDialogService.SetVisibility(true);
                LinkDestinationDialogService.LinksToDeleteSelected += OnDetailDialogServiceLinksToDeleteSelected;
            }
        }
        else
        {
            if (SelectionManager.SelectedConnectorMarker.All(cm => cm.Links.Count == 0))
            {
                foreach (var connectorMarker in SelectionManager.SelectedConnectorMarker)
                    Datastore.Builder.Editors.Connector.SetPublished(connectorMarker.Connector.Connector, false);
            }
            else
            {
                foreach (var connectorMarker in SelectionManager.SelectedConnectorMarker)
                    ConnectorService.DeleteInvisibleLinks(connectorMarker.Links);
            }

            SelectionManager.DeselectAll(SelectionMode.ConnectorMarker);
        }
    }

    public void Dispose()
    {
        _diagram!.Links.Added -= OnDiagramLinksAdded;
        _diagram.Links.Removed -= OnDiagramLinksRemoved;
        _diagram.PointerDown -= OnDiagramPointerDown;
        _diagram.PointerMove -= OnDiagramPointerMove;
        _diagram.PointerUp -= OnDiagramPointerUpAsync;
        _diagram.Nodes.Added -= OnDiagramNodesAdded;
        _diagram.Nodes.Removed -= OnDiagramNodesRemoved;
        _diagram.ZoomChanged -= OnDiagramZoomChanged;

        _labelEditor?.LabelEditorClosed -= OnLabelEditorClosed;

        DiagramEventService.ContainerLoaded -= OnContainerLoaded;
        DiagramEventService.DiagramFocusRequested -= OnDiagramFocusRequestedAsync;
        DiagramEventService.DiagramPointerLeave -= OnDiagramPointerLeave;
        DiagramEventService.EdgeDraggingPointerMove -= OnExternalPointerMoveAsync;
        DiagramEventService.EdgeDraggingPointerUp -= OnExternalPointerUpAsync;
        DiagramEventService.ContextMenuAllowed = () => true;
        DiagramEventService.GridModeChangeRequested -= OnGridModeChangeRequestedAsync;
        DiagramEventService.NodeAlignmentBorderVisibilityChanged -= OnNodeAlignmentBorderVisibilityChangedAsync;
        DiagramEventService.PanBehaviorChangeRequested -= OnPanBehaviorChangeRequested;
        DiagramEventService.SimplifiedViewChangeRequested -= OnSimplifiedViewChangeRequested;
        DiagramEventService.ZoomChanged -= OnDiagramStateZoomChanged;
        DiagramEventService.ZoomToFitRequested -= OnZoomToFitRequested;

        InputEventService.KeyDown -= OnKeyDown;
        InputEventService.PointerUp -= OnExternalPointerUpAsync;
        InputEventService.PointerMove -= OnContainerPointerMoveAsync;

        LibraryService.DragStarted -= OnLibraryDragStarted;
        LibraryService.DragEnded -= OnLibraryDragEnded;
        LibraryService.FunctionBlockCreationRequested -= OnFunctionBlockCreationRequestedAsync;

        LinkDestinationDialogService.LinksToDeleteSelected -= OnDetailDialogServiceLinksToDeleteSelected;

        SelectionManager.DiagramSelectionChanged -= OnDiagramSelectionChanged;

        _dragMovablesBehavior?.Dispose();
        _dragNewLinkBehavior?.Dispose();
        _gimpPanBehavior?.Dispose();
        _gimpZoomBehavior?.Dispose();
        _keyboardBehavior?.Dispose();
        _selectionBehavior?.Dispose();
        _vOPanBehavior?.Dispose();
        _vOZoomBehavior?.Dispose();
        _zoomToFitBehavior?.Dispose();
    }

    private void InitializeDiagram()
    {
        _diagram = new(new()
        {
            AllowMultiSelection = true,
            GridSize = DiagramSettings.DefaultGridSize,
            Links =
            {
                Factory = (diagram, source, targetAnchor) =>
                {
                    if (source is not PortModel sourcePort)
                        throw new InvalidOperationException("Source must be a PortModel");

                    return new BlockNodeLink(new SinglePortAnchor(sourcePort), targetAnchor);
                },
                EnableSnapping = true,
                SnappingRadius = 60
            },
            Virtualization =
            {
                Enabled = false // Might want to change this later when the zoom bug is fixed
            },
            Zoom =
            {
                Inverse = true,
                Maximum = DiagramSettings.ZoomMaximum,
                Minimum = DiagramSettings.ZoomMinimum,
                ScaleFactor = 1.3
            }
        });

        _diagram.SetZoom(DiagramSettings.DefaultZoom);

        _diagram.RegisterComponent<ChildContainerNode, BlockComponent>();
        _diagram.RegisterComponent<LabelNode, LabelComponent>();
        _diagram.RegisterComponent<FunctionBlockNode, BlockComponent>();
        _diagram.RegisterComponent<BlockNodeLink, BlockLinkComponent>();

        _diagram.UnregisterBehavior<PanBehavior>();
        _diagram.UnregisterBehavior<ZoomBehavior>();
        _diagram.UnregisterBehavior<DragNewLinkBehavior>();

        // Um das Default SelectionBehavior austauschen zu können, muss man das DragMovablesBehavior neu binden,
        // ansonsten kommt es zu einer Exception beim Bewegen von Elementen. Diese beiden Behaviors scheinen intern
        // voneinander abzuhängen.
        _diagram.UnregisterBehavior<DragMovablesBehavior>();
        _diagram.UnregisterBehavior<SelectionBehavior>();
        _selectionBehavior = new(_diagram, SelectionManager);
        _diagram.RegisterBehavior(_selectionBehavior);
        _dragMovablesBehavior = new(Datastore, _diagram, DiagramEventService, DiagramService, JSRuntime);
        _diagram.RegisterBehavior(_dragMovablesBehavior);

        _dragNewLinkBehavior = new(Datastore, _diagram, DiagramEventService, DiagramService, InputEventService);
        _diagram.RegisterBehavior(_dragNewLinkBehavior);
        _zoomToFitBehavior = new(_diagram);
        _diagram.RegisterBehavior(_zoomToFitBehavior);

        _diagram.UnregisterBehavior<KeyboardShortcutsBehavior>();
        _keyboardBehavior = new(Datastore, ClusterBuilderEventBuffer, _diagram, DiagramEventService, DiagramService);
        _diagram.RegisterBehavior(_keyboardBehavior);

#if DEBUG
        DiagramService.RequestPanBehaviorChange(true);
#else
        DiagramService.RequestPanBehaviorChange(false);
#endif

        _diagram.Links.Added += OnDiagramLinksAdded;
        _diagram.Links.Removed += OnDiagramLinksRemoved;
        _diagram.PointerDown += OnDiagramPointerDown;
        _diagram.PointerMove += OnDiagramPointerMove;
        _diagram.PointerUp += OnDiagramPointerUpAsync;
        _diagram.Nodes.Added += OnDiagramNodesAdded;
        _diagram.Nodes.Removed += OnDiagramNodesRemoved;
        _diagram.ZoomChanged += OnDiagramZoomChanged;

        DiagramService.Diagram = _diagram;
        DiagramService.DiagramState.IsInitialized = true;
        SelectionManager.AttachDiagramEvents();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
            _labelEditor?.LabelEditorClosed += OnLabelEditorClosed;
    }

    private async Task OnContainerGhostDragEnterAsync(MouseEventArgs e)
    {
        if (LibraryService.DraggingEntries is null)
            return;

        var validDraggingEntries = LibraryService.DraggingEntries
            .Where(le => le.UniqueId != Guid.Empty)
            .ToArray();

        if (validDraggingEntries.Length == 0)
            return;

        var startPosition = _diagram!.GetRelativeGridPoint(new(e.ClientX, e.ClientY), Datastore);
        var nextXPos = startPosition.X;
        var nextYPos = startPosition.Y;
        var index = 1;
        var colCount = Math.Ceiling(Math.Sqrt(validDraggingEntries.Length));
        var maxColHeight = 0.0;

        foreach (var libraryEntry in validDraggingEntries)
        {
            var draggingNode = await Datastore.AddFunctionBlockAsync(DiagramService, libraryEntry.UniqueId, new(nextXPos, nextYPos));
            _diagram!.Nodes.Add(draggingNode);
            _draggingNodes.Add(draggingNode);

            var nodeExtendedSize = draggingNode.GetAlignmentRect();
            if (index % colCount == 0)
            {
                nextXPos = startPosition.X;
                nextYPos += Math.Max(nodeExtendedSize.Height, maxColHeight);
                maxColHeight = 0.0;
            }
            else
            {
                nextXPos += nodeExtendedSize.Width;
                maxColHeight = Math.Max(nodeExtendedSize.Height, maxColHeight);
            }
            index++;
        }

        SelectionManager.SetSelection(_draggingNodes);

        _dragMovablesBehavior!.StartNoJs(e.ClientX, e.ClientY);
    }

    private void OnContainerGhostDragLeave()
    {
        DiagramEventService.InvokeDiagramPointerLeave();

        if (_draggingNodes.Count != 0)
        {
            _dragMovablesBehavior!.End();
            _diagram!.Nodes.Remove(_draggingNodes);
            _draggingNodes.Clear();
        }
    }

    private void OnContainerGhostDragOver(MouseEventArgs e)
        => _dragMovablesBehavior!.Move(e.ClientX, e.ClientY);

    private void OnContainerGhostDrop(MouseEventArgs _)
    {
        _dragMovablesBehavior?.End();

        if (_draggingNodes.Count != 0)
            _draggingNodes.Clear();
    }

    private void OnContainerLoaded(Container container)
    {
        if (container.ViewportX.HasValue && container.ViewportY.HasValue && container.Zoom.HasValue)
        {
            _diagram!.SetPan(container.ViewportX.Value, container.ViewportY.Value);
            _diagram.SetZoom(container.Zoom.Value);
        }
        else
        {
            if (_diagram!.Nodes.Any())
            {
                // At this point the ZoomToFit algorithm calculates a slightly wrong value and the resulting
                // view is a bit too much zoomed in. The problem seems to be that the result of
                // Diagram.Nodes.GetBounds() has 40px too much Height. But why is still unknown.
                // It is a timing problem though, calling the method with a timer yields the correct zoom (but
                // results in visible double rendering).
                DiagramEventService.RequestZoomToFit();
            }
            else
            {
                _diagram!.SetPan(Point.Zero.X, Point.Zero.Y);
                _diagram!.SetZoom(DiagramSettings.DefaultZoom);
            }
        }

        if (DiagramService.Diagram.Nodes.Any() && !DiagramService.Diagram.ArePartialVisibleNodesInViewport(DiagramService.Diagram.Nodes))
            DiagramEventService.RequestZoomToFit();

        if (DiagramService.DiagramState.LabelsLocked)
        {
            foreach (var node in _diagram!.Nodes.OfType<LabelNode>())
            {
                node.Locked = DiagramService.DiagramState.LabelsLocked;
                node.Refresh();
            }
        }
    }

    private void OnContainerPointerDown(PointerEventArgs e)
    {
        if (e.Button != 0)
            return;

        // In Blazor Server ist es möglich, dass beim Laden der Seite die Mouse Events hier schon
        // ankommen bevor das Diagramm (_diagram.Container) vollständig initialisiert ist.
        if (_diagram!.Container is null)
            return;

        _hasPointerDownShiftKey = e.ShiftKey;
        _draggingStartPoint = _diagram.GetRelativeMousePoint(e.ClientX, e.ClientY);
        InputEventService.PointerMove += OnContainerPointerMoveAsync;
        _contextMenuAllowed = false;
        DiagramEventService.RequestEdgeDraggingVisibilityChange(true);
    }

    private async void OnContainerPointerMoveAsync(MouseEventArgs e)
    {
        if (_draggingStartPoint is null)
            return;

        if (e.Buttons != LeftMouseButton)
        {
            _draggingRectangle = null;
            _draggingViewRectangle = null;
            _draggingStartPoint = null;
            InputEventService.PointerMove -= OnContainerPointerMoveAsync;
            await InvokeAsync(StateHasChanged);
            return;
        }

        var relativePoint = _diagram!.GetRelativeMousePoint(e.ClientX, e.ClientY);
        _draggingRectangle = new(
            Math.Min(_draggingStartPoint.X, relativePoint.X),
            Math.Min(_draggingStartPoint.Y, relativePoint.Y),
            Math.Max(_draggingStartPoint.X, relativePoint.X),
            Math.Max(_draggingStartPoint.Y, relativePoint.Y)
        );

        var viewRectTopLeft = _diagram.GetScreenPoint(_draggingRectangle.Left, _draggingRectangle.Top);
        var viewRectBottomRight = _diagram.GetScreenPoint(_draggingRectangle.Right, _draggingRectangle.Bottom);

        _draggingViewRectangle = new(
            viewRectTopLeft.X - _diagram.Container!.Left,
            viewRectTopLeft.Y - _diagram.Container!.Top,
            viewRectBottomRight.X - _diagram.Container!.Left,
            viewRectBottomRight.Y - _diagram.Container!.Top
        );

        await InvokeAsync(StateHasChanged);
    }

    private void OnDetailDialogServiceLinksToDeleteSelected(IEnumerable<Link> links)
    {
        ConnectorService.DeleteInvisibleLinks(links);

        var pubLinkCount = LinkDestinationDialogService.SourceConnectorMarker?.Links.Count;
        if (pubLinkCount < 1)
            SelectionManager.DeselectAll(SelectionMode.ConnectorMarker);
    }

    private async Task OnDiagramFocusRequestedAsync()
        => await JSRuntime.InvokeVoidAsync("ViciOne.Element.focusByClass", "diagram-canvas");

    private void OnDiagramLinksAdded(BaseLinkModel link)
    {
        if (link is not BlockNodeLink fbNodeLink)
            return;

        fbNodeLink.NodeFocusInvoked += OnLinkNodeFocusInvoked;
        fbNodeLink.TargetAttached += OnLinkTargetAttached;

        // Wenn ein Link nicht "attached" ist bedeutet das, dass dieser vom Diagramm
        // generiert wurde und gerade vom Benutzer gezogen wird.
        // Wenn ein Link bereits "attached" ist bedeutet das, dass dieser über den
        // Code hinzugefügt wurde.
        if (!link.IsAttached)
            DiagramService.SetDraggingLink(fbNodeLink);
    }

    private void OnDiagramLinksRemoved(BaseLinkModel link)
    {
        if (DiagramService.DiagramState.SuppressEvents)
            return;

        if (link is not BlockNodeLink fbNodeLink)
            return;

        fbNodeLink.NodeFocusInvoked -= OnLinkNodeFocusInvoked;
        fbNodeLink.TargetAttached -= OnLinkTargetAttached;

        // Links, die nicht "attached" sind, wurden nie dem Datastore hinzugefügt. Höchstwahrscheinlich
        // sind es Links die von Benutzer gezogen wurden oder die generiert wurden, wenn man einen Port
        // selektiert hat.
        if (fbNodeLink.IsAttached)
        {
            if (DiagramService.DiagramState.SimplifiedView)
            {
                fbNodeLink.SourceNode.Refresh();
                fbNodeLink.TargetNode?.Refresh();
            }

            Datastore.Remove(fbNodeLink);
        }
        else
        {
            DiagramService.SetDraggingLink(null);
        }
    }

    private void OnDiagramNodesAdded(NodeModel node)
    {
        if (node is LabelNode labelNode)
        {
            labelNode.EditModeStarted += OnLabelNodeEditModeStarted;
            labelNode.OrderChanged += OnLabelNodeOrderChanged;

            if (!DiagramService.DiagramState.SuppressEvents)
            {
                DiagramService.Diagram.SendToBack(labelNode);
            }
        }
    }

    private void OnDiagramNodesRemoved(NodeModel node)
    {
        if (DiagramService.DiagramState.SuppressEvents)
            return;

        if (node is LabelNode labelNode)
        {
            labelNode.EditModeStarted -= OnLabelNodeEditModeStarted;
            labelNode.OrderChanged -= OnLabelNodeOrderChanged;
            Datastore.Remove(labelNode);
        }
        else if (node is FunctionBlockNode functionBlockNode)
        {
            Datastore.Remove(functionBlockNode);
        }
        else if (node is ChildContainerNode containerNode)
        {
            Datastore.Remove(containerNode);
        }

        UpdatePropertyGrid();
    }

    private void OnDiagramPointerDown(Model? model, global::Blazor.Diagrams.Core.Events.PointerEventArgs _)
    {
        _diagramPointerDownModel = model;
        _diagramPointerMoveFirstMove = true;
    }

    private void OnDiagramPointerLeave()
        => _activePanBehavior?.StopPointerMove();

    private void OnDiagramPointerMove(Model? _1, global::Blazor.Diagrams.Core.Events.PointerEventArgs _2)
    {
        if (_diagramPointerMoveFirstMove)
        {
            if (_diagramPointerDownModel is BlockNodeConnector blockNodeConnector)
                DragService.StartDragging([blockNodeConnector]);

            _diagramPointerMoveFirstMove = false;
        }
    }

    private async void OnDiagramPointerUpAsync(Model? _1, global::Blazor.Diagrams.Core.Events.PointerEventArgs args)
    {
        _contextMenuAllowed = true;
        await ContainerPointerUpAsync();

        RemoveDraggingNodes();

        // Dies hier ist notwendig, da es keine Möglichkeit gibt herauszufinden, ob ein aktuell gezogener
        // Link entgültig an einen Konnektor angeknüpft oder ob er nur temporär durch Snapping an
        // einen Konnektor angefügt wurde. D.h. wir wissen nicht, wann jemand "fertig" ist mit Link ziehen.
        DiagramService.SetDraggingLink(null);

        _diagramPointerDownModel = null;
        _diagramPointerMoveFirstMove = true;
    }

    private void OnDiagramSelectionChanged(SelectableModel model)
    {
        CalculateSelectedModelOrder(model);

        if (DiagramService.DiagramState.SuppressEvents)
            return;

        if (DiagramService.DiagramState.NewlyCreatedLabel is not null &&
            model == DiagramService.DiagramState.NewlyCreatedLabel &&
            !DiagramService.DiagramState.NewlyCreatedLabel.Selected)
        {
            DiagramService.DiagramState.NewlyCreatedLabel.Locked = DiagramService.DiagramState.LabelsLocked;
            DiagramService.DiagramState.NewlyCreatedLabel = null;
        }

        // Da ein Connector (Port) und dessen Published Connector Marker kein SelectableModel sind
        // und somit deren Selektion nicht vom Diagramm verwaltet werden, können wir alle Connectors
        // und deren Published Connector Marker deselektieren wenn sich hier die Selektion ändert.
        if (model is not null)
            SelectionManager.DeselectAll(SelectionMode.FunctionBlockConnector | SelectionMode.ConnectorMarker);

        UpdatePropertyGrid();
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
        => DiagramService.SetZoom(_diagram!.Zoom);

    private async void OnExternalPointerMoveAsync(MouseEventArgs e)
    {
        OnContainerPointerMoveAsync(e);
        await InvokeAsync(StateHasChanged);
    }

    private async void OnExternalPointerUpAsync(MouseEventArgs args)
    {
        _contextMenuAllowed = true;
        await ContainerPointerUpAsync();
        RemoveDraggingNodes();
        _diagram!.Refresh();
        await InvokeAsync(StateHasChanged);
    }

    private async void OnFunctionBlockCreationRequestedAsync(Guid designId)
    {
        var center = _diagram!.GetViewport().Center;
        var fbPosition = new Point(
            center.X - (BlockNodeLayout.Width / 2),
            center.Y - (BlockNodeLayout.DefaultNameHeight + BlockNodeLayout.SettingsRowHeight + (BlockNodeLayout.SystemConnectorRows * BlockNodeLayout.RowHeight)));
        var newNode = await Datastore.AddFunctionBlockAsync(DiagramService, designId, fbPosition);
        newNode.HasPortsInitialized = true;

        _diagram!.Nodes.Add(newNode);
        SelectionManager.SetSelection(newNode);
    }

    private async Task OnGridModeChangeRequestedAsync(GridMode gridMode)
        => await InvokeAsync(StateHasChanged);

    protected override void OnInitialized()
    {
        DiagramEventService.ContainerLoaded += OnContainerLoaded;
        DiagramEventService.ContextMenuAllowed = ContextMenuAllowed;
        DiagramEventService.DiagramFocusRequested += OnDiagramFocusRequestedAsync;
        DiagramEventService.DiagramPointerLeave += OnDiagramPointerLeave;
        DiagramEventService.GridModeChangeRequested += OnGridModeChangeRequestedAsync;
        DiagramEventService.EdgeDraggingPointerMove += OnExternalPointerMoveAsync;
        DiagramEventService.EdgeDraggingPointerUp += OnExternalPointerUpAsync;
        DiagramEventService.NodeAlignmentBorderVisibilityChanged += OnNodeAlignmentBorderVisibilityChangedAsync;
        DiagramEventService.PanBehaviorChangeRequested += OnPanBehaviorChangeRequested;
        DiagramEventService.SimplifiedViewChangeRequested += OnSimplifiedViewChangeRequested;
        DiagramEventService.ZoomChanged += OnDiagramStateZoomChanged;
        DiagramEventService.ZoomToFitRequested += OnZoomToFitRequested;

        InputEventService.KeyDown += OnKeyDown;
        InputEventService.PointerUp += OnExternalPointerUpAsync;

        LibraryService.DragEnded += OnLibraryDragEnded;
        LibraryService.DragStarted += OnLibraryDragStarted;
        LibraryService.FunctionBlockCreationRequested += OnFunctionBlockCreationRequestedAsync;

        SelectionManager.DiagramSelectionChanged += OnDiagramSelectionChanged;

        InitializeDiagram();
    }

    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Code == KeyboardCodes.Delete)
            DeleteSelectedConnectorMarkerLinks();
    }

    private void OnLabelEditorClosed(string content)
    {
        if (_editingLabel is null)
            return;

        Datastore.Builder.Editors.Label.SetContent(_editingLabel, content);

        _editingLabel = null;
    }

    private void OnLabelNodeEditModeStarted(LabelNode labelNode)
    {
        var model = Datastore.DataflowDiagramMapping.GetModel(labelNode);
        _editingLabel = model;

        var content = _editingLabel.Content ?? string.Empty;

        var _ = _labelEditor!.Show(content);
    }

    private void OnLabelNodeOrderChanged(SelectableModel model)
    {
        if (DiagramService.DiagramState.SuppressEvents)
            return;

        Datastore.Builder.Editors.Label.SetZIndex(Datastore.DataflowDiagramMapping.GetModel((model as LabelNode)!), model.Order);
    }

    private void OnLibraryDragEnded()
    {
        _libraryDragInProgress = false;
        InvokeAsync(StateHasChanged);
    }

    private void OnLibraryDragStarted()
    {
        _libraryDragInProgress = true;
        InvokeAsync(StateHasChanged);
    }

    private void OnLinkNodeFocusInvoked(LinkModel link, NodeModel node)
    {
        SelectionManager.DeselectAll();
        _diagram!.SelectModel(link, false);
        _diagram.SelectModel(node, false);

        if (!_diagram.IsNodeInViewport(node))
            _diagram.PanToNode(node);
    }

    private void OnLinkTargetAttached(BaseLinkModel link)
    {
        if (link is not BlockNodeLink nodeLink)
            return;

        if (Datastore.AddLink(nodeLink))
        {
            nodeLink.DrawOverlay = true;
            nodeLink.Refresh();
        }
        else
        {
            DiagramService.DiagramState.SuppressEvents = true;
            _diagram!.Links.Remove(nodeLink);
            DiagramService.DiagramState.SuppressEvents = false;
        }
    }

    private async void OnNodeAlignmentBorderVisibilityChangedAsync(bool isVisible)
    {
        _isNodeAlignmentBorderVisible = isVisible;
        await InvokeAsync(StateHasChanged);
    }

    internal void OnPanBehaviorChangeRequested(bool useGimpPanBehavior)
    {
        if (useGimpPanBehavior)
        {
            if (_gimpPanBehavior is not null)
                return;

            _diagram!.UnregisterBehavior<VOPanBehavior>();
            _vOPanBehavior?.Dispose();
            _vOPanBehavior = null;
            _gimpPanBehavior = new(_diagram, DiagramEventService);
            _diagram.RegisterBehavior(_gimpPanBehavior);

            _diagram.UnregisterBehavior<VOZoomBehavior>();
            _vOZoomBehavior?.Dispose();
            _vOZoomBehavior = null;
            _gimpZoomBehavior = new(_diagram, DiagramEventService);
            _diagram.RegisterBehavior(_gimpZoomBehavior);

            _activePanBehavior = _gimpPanBehavior;
        }
        else
        {
            if (_vOPanBehavior is not null)
                return;

            _diagram!.UnregisterBehavior<GimpPanBehavior>();
            _gimpPanBehavior?.Dispose();
            _gimpPanBehavior = null;
            _vOPanBehavior = new(_diagram, DiagramEventService);
            _diagram.RegisterBehavior(_vOPanBehavior);

            _diagram.UnregisterBehavior<GimpZoomBehavior>();
            _gimpZoomBehavior?.Dispose();
            _gimpZoomBehavior = null;
            _vOZoomBehavior = new(_diagram, DiagramEventService);
            _diagram.RegisterBehavior(_vOZoomBehavior);

            _activePanBehavior = _vOPanBehavior;
        }
    }

    private void OnSimplifiedViewChangeRequested(bool simplifiedView)
    {
        foreach (var node in _diagram!.Nodes)
            node.Refresh();
    }

    private void OnZoomToFitRequested()
        => _zoomToFitBehavior!.ZoomToFit();

    private void RemoveDraggingNodes()
    {
        if (_draggingNodes.Count == 0)
            return;

        foreach (var node in _draggingNodes)
        {
            node.ReinitializePorts();
            node.HasPortsInitialized = true;
        }

        _draggingNodes.Clear();
        DiagramEventService.RequestDiagramFocus();

        LibraryService.DraggingEntries = null;
        _dragMovablesBehavior!.End();
    }

    private void SetDiagramViewport(Rectangle rect)
        => _diagram!.Batch(() =>
        {
            _diagram.SetZoom(Math.Min(
                Math.Min(
                    _diagram.Container!.Width / rect.Width,
                    _diagram.Container.Height / rect.Height
                ),
                _diagram.Options.Zoom.Maximum)
            );

            _diagram.UpdatePan(
                -_diagram.Pan.X - (rect.Left * _diagram.Zoom),
                -_diagram.Pan.Y - (rect.Top * _diagram.Zoom)
            );
        });

    private async Task ShowContextMenuAsync(MouseEventArgs e)
    {
        if (ContextMenuAllowed())
            await ContextMenuRequest.SendAsync(new() { ItemFilter = SelectionManager.GetContextMenuItemFilterForSelection(), MouseEventArgs = e });
    }

    private void UpdatePropertyGrid()
    {
        var selectedContainers = Datastore.DataflowDiagramMapping.GetModels(SelectionManager.SelectedContainers);
        var selectedFunctionBlocks = Datastore.DataflowDiagramMapping.GetModels(SelectionManager.SelectedFBs);
        var selectedLinks = Datastore.DataflowDiagramMapping.GetModels(SelectionManager.SelectedLinks);
        var selectedLabels = Datastore.DataflowDiagramMapping.GetModels(SelectionManager.SelectedLabels);

        var instances = Array.Empty<object>()
            .Concat(selectedContainers)
            .Concat(selectedLabels)
            .Concat(selectedFunctionBlocks)
            .Concat(selectedLinks);

        PropertyGridController.SetInstances(instances, new DataflowToolbarPropertyGridContext());
    }
}
