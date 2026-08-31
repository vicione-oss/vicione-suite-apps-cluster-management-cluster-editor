using System;
using System.Threading.Tasks;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models.Base;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components.NodeEditorServices;

internal sealed class DiagramPointerInteractionController(
    DiagramService diagramService,
    SelectionManager selectionManager,
    InputEventService inputEventService,
    DiagramEventService diagramEventService,
    DragService dragService,
    ILogger<NodeEditor> logger,
    NodeEditorBehaviorController behaviorController) : IDisposable
{
    private const int LeftMouseButton = 1;

    private bool _contextMenuAllowed = true;
    private BlazorDiagram? _diagram;
    private Model? _diagramPointerDownModel;
    private bool _diagramPointerMoveFirstMove;
    private Rectangle? _draggingRectangle;
    private Point? _draggingStartPoint;
    private Rectangle? _draggingViewRectangle;
    private bool _hasPointerDownShiftKey;
    private bool _initialized;
    private Action _requestRender = () => { };

    public Rectangle? ViewRectangle => _draggingViewRectangle;

    private async Task ContainerPointerUp()
    {
        diagramEventService.RequestEdgeDraggingVisibilityChange(false);
        inputEventService.PointerMove -= OnContainerPointerMove;

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

            await selectionManager.SelectInRectangle(mode, _draggingRectangle);
        }

        _draggingRectangle = null;
        _draggingViewRectangle = null;
        _draggingStartPoint = null;

        _requestRender();
    }

    public bool ContextMenuAllowed()
        => _contextMenuAllowed && !behaviorController.IsMoving;

    public void Dispose()
        => Teardown();

    public void Initialize(BlazorDiagram diagram, Action requestRender)
    {
        if (_initialized)
            Teardown();

        _diagram = diagram;
        _requestRender = requestRender;

        _diagram.PointerDown += OnDiagramPointerDown;
        _diagram.PointerMove += OnDiagramPointerMove;
        _diagram.PointerUp += OnDiagramPointerUp;

        inputEventService.PointerUp += OnExternalPointerUp;

        diagramEventService.EdgeDraggingPointerMove += OnExternalPointerMove;
        diagramEventService.EdgeDraggingPointerUp += OnExternalPointerUp;

        _initialized = true;
    }

    public void OnContainerPointerDown(PointerEventArgs e)
    {
        if (e.Button != 0)
            return;

        // In Blazor Server ist es möglich, dass beim Laden der Seite die Mouse Events hier schon
        // ankommen bevor das Diagramm (_diagram.Container) vollständig initialisiert ist.
        if (_diagram!.Container is null)
            return;

        _hasPointerDownShiftKey = e.ShiftKey;
        _draggingStartPoint = _diagram.GetRelativeMousePoint(e.ClientX, e.ClientY);
        inputEventService.PointerMove += OnContainerPointerMove;
        _contextMenuAllowed = false;
        diagramEventService.RequestEdgeDraggingVisibilityChange(true);
    }

    private void OnContainerPointerMove(PointerEventArgs e)
        => AsyncGuard.SafeFireAndForget(() => OnContainerPointerMoveCore(e), logger);

    private async Task OnContainerPointerMoveCore(PointerEventArgs e)
    {
        if (_draggingStartPoint is null)
            return;

        if (_diagram is null)
            return;

        if (e.Buttons != LeftMouseButton)
        {
            _draggingRectangle = null;
            _draggingViewRectangle = null;
            _draggingStartPoint = null;
            inputEventService.PointerMove -= OnContainerPointerMove;
            _requestRender();
            return;
        }

        var relativePoint = _diagram.GetRelativeMousePoint(e.ClientX, e.ClientY);
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

        _requestRender();
    }

    private void OnDiagramPointerDown(Model? model, global::Blazor.Diagrams.Core.Events.PointerEventArgs _)
    {
        _diagramPointerDownModel = model;
        _diagramPointerMoveFirstMove = true;
    }

    private void OnDiagramPointerMove(Model? _1, global::Blazor.Diagrams.Core.Events.PointerEventArgs _2)
    {
        if (_diagramPointerMoveFirstMove)
        {
            if (_diagramPointerDownModel is BlockNodeConnector blockNodeConnector)
                dragService.StartDragging([blockNodeConnector]);

            _diagramPointerMoveFirstMove = false;
        }
    }

    private void OnDiagramPointerUp(Model? _1, global::Blazor.Diagrams.Core.Events.PointerEventArgs _2)
        => AsyncGuard.SafeFireAndForget(OnDiagramPointerUpCore, logger);

    private async Task OnDiagramPointerUpCore()
    {
        _contextMenuAllowed = true;
        await ContainerPointerUp();

        // Dies hier ist notwendig, da es keine Möglichkeit gibt herauszufinden, ob ein aktuell gezogener
        // Link entgültig an einen Konnektor angeknüpft oder ob er nur temporär durch Snapping an
        // einen Konnektor angefügt wurde. D.h. wir wissen nicht, wann jemand "fertig" ist mit Link ziehen.
        diagramService.SetDraggingLink(null);

        _diagramPointerDownModel = null;
        _diagramPointerMoveFirstMove = true;
    }

    private void OnExternalPointerMove(PointerEventArgs e)
        => AsyncGuard.SafeFireAndForget(() => OnExternalPointerMoveCore(e), logger);

    private async Task OnExternalPointerMoveCore(PointerEventArgs e)
    {
        await OnContainerPointerMoveCore(e);
        _requestRender();
    }

    private void OnExternalPointerUp(PointerEventArgs _)
        => AsyncGuard.SafeFireAndForget(OnExternalPointerUpCore, logger);

    private async Task OnExternalPointerUpCore()
    {
        if (_diagram is null)
            return;

        _contextMenuAllowed = true;
        await ContainerPointerUp();
        _diagram.Refresh();
        _requestRender();
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

    private void Teardown()
    {
        if (_diagram is not null)
        {
            _diagram.PointerDown -= OnDiagramPointerDown;
            _diagram.PointerMove -= OnDiagramPointerMove;
            _diagram.PointerUp -= OnDiagramPointerUp;
        }

        inputEventService.PointerUp -= OnExternalPointerUp;
        inputEventService.PointerMove -= OnContainerPointerMove;

        diagramEventService.EdgeDraggingPointerMove -= OnExternalPointerMove;
        diagramEventService.EdgeDraggingPointerUp -= OnExternalPointerUp;

        _diagram = null;
        _requestRender = () => { };
        _initialized = false;
    }
}
