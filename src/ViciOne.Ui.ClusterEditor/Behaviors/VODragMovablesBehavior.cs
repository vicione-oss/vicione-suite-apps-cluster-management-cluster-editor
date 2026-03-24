using System.Collections.Generic;
using System.Linq;
using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models.Base;
using Microsoft.JSInterop;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Behaviors;

// This code was originally inspired by DragMovablesBehavior.cs in
// Blazor.Diagrams.Core.Behaviors
internal sealed class VODragMovablesBehavior : Behavior
{
    private readonly IDatastore _datastore;
    private readonly DiagramEventService _diagramEventService;
    private readonly DiagramService _diagramService;
    private Dictionary<MovableModel, Point> _initialModelPositions = [];
    private Point _initialPointerPosition = Point.Zero;
    private readonly IJSRuntime _jSRuntime;
    private bool _modelWasMoved;
    private DotNetObjectReference<VODragMovablesBehavior>? _refObject;

    public bool IsMoving => _initialModelPositions.Count > 0;

    public VODragMovablesBehavior(
        IDatastore datastore,
        Diagram diagram,
        DiagramEventService diagramEventService,
        DiagramService diagramService,
        IJSRuntime jSRuntime) : base(diagram)
    {
        _datastore = datastore;
        _diagramEventService = diagramEventService;
        _diagramService = diagramService;
        _jSRuntime = jSRuntime;

        Diagram.PanChanged += OnDiagramPanChanged;
        Diagram.PointerDown += OnDiagramPointerDown;
        Diagram.PointerUp += OnDiagramPointerUp;
        Diagram.ZoomChanged += OnDiagramZoomChanged;

        _diagramEventService.ContainerLoaded += OnContainerLoaded;
        _diagramEventService.EdgeDraggingPointerUp += OnEdgeDraggingPointerUp;
    }

    public override void Dispose()
    {
        Diagram.PanChanged -= OnDiagramPanChanged;
        Diagram.PointerDown -= OnDiagramPointerDown;
        Diagram.PointerUp -= OnDiagramPointerUp;
        Diagram.ZoomChanged -= OnDiagramZoomChanged;

        _diagramEventService.ContainerLoaded -= OnContainerLoaded;
        _diagramEventService.EdgeDraggingPointerUp -= OnEdgeDraggingPointerUp;

        _refObject?.Dispose();
    }

    public void End()
    {
        if (_modelWasMoved)
        {
            foreach (var model in _initialModelPositions.Keys)
            {
                switch (model)
                {
                    case ChildContainerNode ccNode:
                        ChildContainerMapper.UpdatePosition(_datastore, ccNode);
                        break;
                    case FunctionBlockNode fbNode:
                        FunctionBlockMapper.UpdatePosition(_datastore, fbNode);

                        // Wird benötigt, da beim Ziehen von FBs aus der FB-Lib das Diagramm einen Resize-Observer startet
                        // und dieser irgendwann das Observe-Callback ruft, was zu falschen Portpositionen während des Ziehens
                        // führt. Hier ist nun die Endposition bekannt und die Portpositionen können genau bestimmt werden.
                        // ! Diese Operation führt zu einem JS Call für jeden Port, dies kann sehr schnell zeitaufwendig werden.
                        if (!fbNode.HasPortsInitialized)
                        {
                            fbNode.ReinitializePorts();
                            fbNode.HasPortsInitialized = true;
                        }
                        break;
                    case LabelNode labelNode:
                        LabelMapper.UpdatePosition(_datastore, labelNode);
                        break;
                    default:
                        break;
                }
            }
        }

        Reset();
    }

    private void EndMove(double clientX, double clientY)
    {
        if (!IsMoving)
            return;

        Move(clientX, clientY);

        var _ = _jSRuntime.InvokeVoidAsync("ViciOne.NodeMove._end", clientX, clientY);

        End();
    }

    [JSInvokable]
    public void JsFirstMove()
    {
        if (!IsMoving)
            return;

        _diagramService.SetNodeAlignmentBorderVisibility(true);
        _diagramEventService.RequestEdgeDraggingVisibilityChange(true);
    }

    public void Move(double clientX, double clientY)
    {
        if (!IsMoving)
            return;

        var currentPointerPosition = Diagram.GetRelativeGridPoint(new(clientX, clientY), _datastore);

        var currentDelta = new Point(
            currentPointerPosition.X - _initialPointerPosition.X,
            currentPointerPosition.Y - _initialPointerPosition.Y
        );

        foreach (var (model, initialPos) in _initialModelPositions)
        {
            var nx = initialPos.X + currentDelta.X;
            var ny = initialPos.Y + currentDelta.Y;

            if (nx == model.Position.X && ny == model.Position.Y)
                continue;

            model.SetPosition(nx, ny);

            // This is a little simplification here, we assume that when one model was moved, all of them were moved
            // But this should be fine, I can't think of an instance where this wouldn't hold
            _modelWasMoved = true;
        }
    }

    private void OnContainerLoaded(Cluster.Model.Container _)
        => Reset();

    private void OnDiagramPanChanged()
    {
        if (!IsMoving)
            return;

        var _ = _jSRuntime.InvokeVoidAsync("ViciOne.NodeMove.diagramPanChanged", Diagram.Pan.X, Diagram.Pan.Y);
    }

    private void OnDiagramPointerDown(Model? model, global::Blazor.Diagrams.Core.Events.PointerEventArgs e)
    {
        if (e.Button != 0 || model is not MovableModel)
            return;

        Start(e.ClientX, e.ClientY);
    }

    private void OnDiagramPointerUp(Model? _, global::Blazor.Diagrams.Core.Events.PointerEventArgs e)
        => EndMove(e.ClientX, e.ClientY);

    private void OnDiagramZoomChanged()
    {
        if (!IsMoving)
            return;

        var _ = _jSRuntime.InvokeVoidAsync("ViciOne.NodeMove.diagramZoomChanged", Diagram.Zoom);
    }

    private void OnEdgeDraggingPointerUp(Microsoft.AspNetCore.Components.Web.MouseEventArgs e)
        => EndMove(e.ClientX, e.ClientY);

    private void Reset()
    {
        _initialPointerPosition = Point.Zero;
        _initialModelPositions = [];
        _modelWasMoved = false;

        _diagramService.SetNodeAlignmentBorderVisibility(false);
        _diagramEventService.RequestEdgeDraggingVisibilityChange(false);
    }

    private void Start(double clientX, double clientY)
    {
        if (IsMoving)
            return;

        _initialModelPositions = Diagram.GetSelectedModels().OfType<MovableModel>()
            .Where(m => !m.Locked)
            .ToDictionary(m => m, m => m.Position);

        if (_initialModelPositions.Count == 0)
            return;

        _initialPointerPosition = Diagram.GetRelativeGridPoint(new(clientX, clientY), _datastore);
        _modelWasMoved = false;

        var relevantLinks = Diagram.Links.OfType<BlockNodeLink>().Where(l => _initialModelPositions.Keys.Any(m => m.Id == l.SourceNode.Id || m.Id == l.TargetNode.Id)).ToArray();
        var sourceMarkerWidth = relevantLinks.FirstOrDefault()?.SourceMarker?.Width ?? 0;
        var targetMarkerWidth = relevantLinks.FirstOrDefault()?.TargetMarker?.Width ?? 0;
        var linkIds = relevantLinks.Select(l => l.Id);
        var linkSourceIds = relevantLinks.Select(l => l.SourceNode.Id);
        var linkSourcePosXs = relevantLinks.Select(l => l.SourcePort!.MiddlePosition.X - l.SourceNode.Position.X + (l.SourcePort.Size.Width / 2) + sourceMarkerWidth);
        var linkSourcePosYs = relevantLinks.Select(l => l.SourcePort!.MiddlePosition.Y - l.SourceNode.Position.Y);
        var linkTargetIds = relevantLinks.Select(l => l.TargetNode.Id);
        var linkTargetPosXs = relevantLinks.Select(l => l.TargetPort!.MiddlePosition.X - l.TargetNode.Position.X - (l.TargetPort.Size.Width / 2) - targetMarkerWidth);
        var linkTargetPosYs = relevantLinks.Select(l => l.TargetPort!.MiddlePosition.Y - l.TargetNode.Position.Y);

        var gridSize = _datastore.Builder.Settings.GridSize;
        var movableModelIds = _initialModelPositions.Keys.Select(m => m.Id);

        double[] containerPos = [Diagram.Container?.Left ?? 0, Diagram.Container?.Top ?? 0];
        double[] pan = [Diagram.Pan.X, Diagram.Pan.Y];
        double[] initPos = [_initialPointerPosition.X, _initialPointerPosition.Y];

        _refObject ??= DotNetObjectReference.Create(this);

        var _ = _jSRuntime.InvokeVoidAsync("ViciOne.NodeMove.start",
            linkIds,
            linkSourceIds,
            linkSourcePosXs,
            linkSourcePosYs,
            linkTargetIds,
            linkTargetPosXs,
            linkTargetPosYs,
            _refObject,
            gridSize,
            movableModelIds,
            Diagram.Zoom,
            containerPos,
            pan,
            initPos);
    }

    public void StartNoJs(double clientX, double clientY)
    {
        _initialModelPositions = Diagram.GetSelectedModels().OfType<MovableModel>().Where(m => !m.Locked).ToDictionary(m => m, m => m.Position);
        if (_initialModelPositions.Count == 0)
            return;

        _initialPointerPosition = Diagram.GetRelativeGridPoint(new(clientX, clientY), _datastore);
        _modelWasMoved = false;

        _diagramService.SetNodeAlignmentBorderVisibility(true);
        _diagramEventService.RequestEdgeDraggingVisibilityChange(true);
    }
}
