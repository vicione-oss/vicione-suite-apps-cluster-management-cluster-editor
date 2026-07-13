using System.Collections.Generic;
using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models.Base;
using Microsoft.JSInterop;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
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
    private readonly InputEventService _inputEventService;
    private readonly IJSRuntime _jSRuntime;
    private bool _modelWasMoved;
    private DotNetObjectReference<VODragMovablesBehavior>? _refObject;

    public bool IsMoving => _initialModelPositions.Count > 0;

    public VODragMovablesBehavior(
        IDatastore datastore,
        Diagram diagram,
        DiagramEventService diagramEventService,
        DiagramService diagramService,
        InputEventService inputEventService,
        IJSRuntime jSRuntime) : base(diagram)
    {
        _datastore = datastore;
        _diagramEventService = diagramEventService;
        _diagramService = diagramService;
        _inputEventService = inputEventService;
        _jSRuntime = jSRuntime;

        Diagram.PanChanged += OnDiagramPanChanged;
        Diagram.PointerDown += OnDiagramPointerDown;
        Diagram.PointerUp += OnDiagramPointerUp;
        Diagram.ZoomChanged += OnDiagramZoomChanged;

        _diagramEventService.ContainerLoaded += OnContainerLoaded;
        _diagramEventService.EdgeDraggingPointerUp += OnEdgeDraggingPointerUp;

        _inputEventService.PointerLeave += OnInputEventServicePointerLeave;
        _inputEventService.PointerUp += OnInputEventServicePointerUp;
    }

    public override void Dispose()
    {
        Diagram.PanChanged -= OnDiagramPanChanged;
        Diagram.PointerDown -= OnDiagramPointerDown;
        Diagram.PointerUp -= OnDiagramPointerUp;
        Diagram.ZoomChanged -= OnDiagramZoomChanged;

        _diagramEventService.ContainerLoaded -= OnContainerLoaded;
        _diagramEventService.EdgeDraggingPointerUp -= OnEdgeDraggingPointerUp;

        _inputEventService.PointerLeave -= OnInputEventServicePointerLeave;
        _inputEventService.PointerUp -= OnInputEventServicePointerUp;

        _refObject?.Dispose();
        _refObject = null;
    }

    private void End()
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

    public void EndMove(double clientX, double clientY)
    {
        if (!IsMoving)
            return;

        Move(clientX, clientY);

        _ = _jSRuntime.InvokeVoidAsync("ViciOne.NodeMove.end").AsTask();

        End();
    }

    public void ExternalMove(double clientX, double clientY)
    {
        if (!IsMoving)
            return;

        _ = _jSRuntime.InvokeVoidAsync("ViciOne.NodeMove.externalMove", clientX, clientY).AsTask();
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

        _ = _jSRuntime.InvokeVoidAsync("ViciOne.NodeMove.diagramPanChanged", Diagram.Pan.X, Diagram.Pan.Y).AsTask();
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

        _ = _jSRuntime.InvokeVoidAsync("ViciOne.NodeMove.diagramZoomChanged", Diagram.Zoom).AsTask();
    }

    private void OnEdgeDraggingPointerUp(Microsoft.AspNetCore.Components.Web.PointerEventArgs e)
        => EndMove(e.ClientX, e.ClientY);

    private void OnInputEventServicePointerLeave(Microsoft.AspNetCore.Components.Web.PointerEventArgs e)
        => EndMove(e.ClientX, e.ClientY);

    private void OnInputEventServicePointerUp(Microsoft.AspNetCore.Components.Web.PointerEventArgs e)
        => EndMove(e.ClientX, e.ClientY);

    private void Reset()
    {
        _initialPointerPosition = Point.Zero;
        _initialModelPositions = [];
        _modelWasMoved = false;

        _diagramService.SetNodeAlignmentBorderVisibility(false);
        _diagramEventService.RequestEdgeDraggingVisibilityChange(false);
    }

    public void Start(double clientX, double clientY, bool useJsDomEvents = true)
    {
        if (IsMoving)
            return;

        _initialModelPositions = [];
        foreach (var model in Diagram.GetSelectedModels())
        {
            if (model is MovableModel movable && !movable.Locked)
                _initialModelPositions[movable] = movable.Position;
        }

        if (_initialModelPositions.Count == 0)
            return;

        _initialPointerPosition = Diagram.GetRelativeGridPoint(new(clientX, clientY), _datastore);
        _modelWasMoved = false;

        var selectedIds = new HashSet<string>(_initialModelPositions.Count);
        var movableModelIds = new string[_initialModelPositions.Count];
        var i = 0;
        foreach (var model in _initialModelPositions.Keys)
        {
            selectedIds.Add(model.Id);
            movableModelIds[i++] = model.Id;
        }

        List<BlockNodeLink> relevantLinks = [];
        foreach (var link in Diagram.Links)
        {
            if (link is BlockNodeLink blockLink && (selectedIds.Contains(blockLink.SourceNode.Id) || selectedIds.Contains(blockLink.TargetNode.Id)))
                relevantLinks.Add(blockLink);
        }

        var sourceMarkerWidth = relevantLinks.Count > 0 ? relevantLinks[0].SourceMarker?.Width ?? 0 : 0;
        var targetMarkerWidth = relevantLinks.Count > 0 ? relevantLinks[0].TargetMarker?.Width ?? 0 : 0;

        var linkIds = new string[relevantLinks.Count];
        var linkSourceIds = new string[relevantLinks.Count];
        var linkSourcePosXs = new double[relevantLinks.Count];
        var linkSourcePosYs = new double[relevantLinks.Count];
        var linkTargetIds = new string[relevantLinks.Count];
        var linkTargetPosXs = new double[relevantLinks.Count];
        var linkTargetPosYs = new double[relevantLinks.Count];

        for (var j = 0; j < relevantLinks.Count; j++)
        {
            var l = relevantLinks[j];
            linkIds[j] = l.Id;
            linkSourceIds[j] = l.SourceNode.Id;
            linkSourcePosXs[j] = l.SourcePort!.MiddlePosition.X - l.SourceNode.Position.X + (l.SourcePort.Size.Width / 2) + sourceMarkerWidth;
            linkSourcePosYs[j] = l.SourcePort!.MiddlePosition.Y - l.SourceNode.Position.Y;
            linkTargetIds[j] = l.TargetNode.Id;
            linkTargetPosXs[j] = l.TargetPort!.MiddlePosition.X - l.TargetNode.Position.X - (l.TargetPort.Size.Width / 2) - targetMarkerWidth;
            linkTargetPosYs[j] = l.TargetPort!.MiddlePosition.Y - l.TargetNode.Position.Y;
        }

        var gridSize = _datastore.Builder.Settings.GridSize;

        double[] containerPos = [Diagram.Container?.Left ?? 0, Diagram.Container?.Top ?? 0];
        double[] pan = [Diagram.Pan.X, Diagram.Pan.Y];
        double[] initPos = [_initialPointerPosition.X, _initialPointerPosition.Y];

        _refObject ??= DotNetObjectReference.Create(this);

        _ = _jSRuntime.InvokeVoidAsync("ViciOne.NodeMove.start",
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
            initPos,
            useJsDomEvents).AsTask();
    }
}
