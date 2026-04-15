using System;
using System.Linq;
using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Behaviors;

// Der Code dieser Klasse wurde zum Großteil von DragNewLinkBehavior aus der
// Blazor.Diagrams Library übernommen und nur angepasst
internal class VODragNewLinkBehavior : Behavior
{
    private Model? _currentModel;
    private readonly IDatastore _datastore;
    private readonly DiagramEventService _diagramEventService;
    private readonly DiagramService _diagramService;
    private readonly InputEventService _inputEventService;
    private BaseLinkModel? _ongoingLink;
    private PositionAnchor? _targetPositionAnchor;

    public VODragNewLinkBehavior(
        IDatastore datastore,
        Diagram diagram,
        DiagramEventService diagramEventService,
        DiagramService diagramService,
        InputEventService inputEventService) : base(diagram)
    {
        _datastore = datastore;
        _diagramEventService = diagramEventService;
        _diagramService = diagramService;
        _inputEventService = inputEventService;

        Diagram.PointerDown += OnPointerDown;
    }

    private void DetachEvents()
    {
        Diagram.PointerUp -= OnPointerUp;
        _diagramEventService.EdgeDraggingPointerMove -= OnEdgeDraggingPointerMove;
        _diagramEventService.EdgeDraggingPointerUp -= OnExternalPointerUp;
        _inputEventService.KeyDown -= OnKeyDown;
        _inputEventService.PointerMove -= OnPointerMove;
        _inputEventService.PointerUp -= OnExternalPointerUp;
    }

    public override void Dispose()
    {
        Diagram.PointerDown -= OnPointerDown;

        GC.SuppressFinalize(this);
    }

    private void End(Model? model)
    {
        if (_ongoingLink is null)
            return;

        DetachEvents();

        if (_ongoingLink.IsAttached) // Snapped already
        {
            _ongoingLink.TriggerTargetAttached();
            _ongoingLink = null;
            return;
        }

        PortModel? targetPort = null;

        if (model is BlockNodeLink otherLink && otherLink != _ongoingLink)
        {
            targetPort = GetTargetPortFromOtherLink(otherLink);
        }
        else if (model is PortModel port && _ongoingLink.Source.Model!.CanAttachTo(port))
        {
            targetPort = port;
        }

        if (targetPort is null)
        {
            Diagram.Links.Remove(_ongoingLink);
        }
        else
        {
            _ongoingLink.SetTarget(new SinglePortAnchor(targetPort));
            _ongoingLink.TriggerTargetAttached();
            _ongoingLink.Refresh();
            targetPort.Refresh();
            (_ongoingLink.Source.Model as PortModel)!.Parent.Group?.Refresh();
            targetPort.Parent.Group?.Refresh();
        }

        _ongoingLink = null;
    }

    private PortModel? FindNearPortToAttachTo()
    {
        if (_ongoingLink is null || _targetPositionAnchor is null)
            return null;

        PortModel? nearestSnapPort = null;
        var nearestSnapPortDistance = double.PositiveInfinity;

        var position = _targetPositionAnchor!.GetPosition(_ongoingLink)!;

        foreach (var port in Diagram.Nodes.SelectMany(n => n.Ports))
        {
            var distance = position.DistanceTo(port.Position);

            if (distance <= Diagram.Options.Links.SnappingRadius && (_ongoingLink.Source.Model?.CanAttachTo(port) != false))
            {
                if (distance < nearestSnapPortDistance)
                {
                    nearestSnapPortDistance = distance;
                    nearestSnapPort = port;
                }
            }
        }

        return nearestSnapPort;
    }

    private PortModel? GetTargetPortFromOtherLink(BlockNodeLink otherLink)
    {
        var sourcePort = (_ongoingLink as BlockNodeLink)!.SourcePort;
        if (sourcePort is null)
            return null;

        var sourceConnector = (IConnectorOutput)_datastore.DataflowDiagramMapping.GetModel((BlockNodeConnector)sourcePort);
        var destinationConnector = (IConnectorInput)_datastore.DataflowDiagramMapping.GetModel((BlockNodeConnector)otherLink.TargetPort!);

        if (!_datastore.Builder.Editors.Connector.CanCreateLink(sourceConnector, destinationConnector))
            return null;

        return otherLink.TargetPort!;
    }

    private void Move(double clientX, double clientY)
    {
        if (_ongoingLink is null)
            return;

        _targetPositionAnchor!.SetPosition(Diagram.GetRelativeMousePoint(clientX, clientY).Subtract(5.0));

        if (Diagram.Options.Links.EnableSnapping)
        {
            var portModel = FindNearPortToAttachTo();
            if (portModel is not null || _ongoingLink!.Target is not PositionAnchor)
            {
                _ongoingLink!.SetTarget((portModel is null) ? _targetPositionAnchor : new SinglePortAnchor(portModel));
            }
        }

        _ongoingLink.Refresh();
    }

    private void OnEdgeDraggingPointerMove(PointerEventArgs e)
        => Move(e.ClientX, e.ClientY);

    private void OnExternalPointerUp(PointerEventArgs _)
    {
        if (_currentModel is null)
            return;

        End(_currentModel);
        _diagramService.SetDraggingLink(null);
    }

    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Code != KeyboardCodes.Escape || _ongoingLink is null)
            return;

        DetachEvents();

        if (_ongoingLink.IsAttached)
            _ongoingLink.SetTarget(new PositionAnchor(new(int.MaxValue, int.MaxValue)));

        Diagram.Links.Remove(_ongoingLink);

        _ongoingLink = null;
    }

    private void OnPointerDown(Model? model, global::Blazor.Diagrams.Core.Events.PointerEventArgs e)
    {
        if (e.Button != (int)MouseEventButton.Left)
            return;

        Start(model, e.ClientX, e.ClientY);
    }

    private void OnPointerMove(PointerEventArgs e)
        => Move(e.ClientX, e.ClientY);

    private void OnPointerUp(Model? model, global::Blazor.Diagrams.Core.Events.PointerEventArgs e)
        => End(model);

    private void Start(Model? model, double clientX, double clientY)
    {
        if (model is not PortModel port || port.Locked)
            return;

        Diagram.PointerUp += OnPointerUp;
        _diagramEventService.EdgeDraggingPointerMove += OnEdgeDraggingPointerMove;
        _diagramEventService.EdgeDraggingPointerUp += OnExternalPointerUp;
        _inputEventService.KeyDown += OnKeyDown;
        _inputEventService.PointerMove += OnPointerMove;
        _inputEventService.PointerUp += OnExternalPointerUp;

        _currentModel = model;
        _targetPositionAnchor = new PositionAnchor(Diagram.GetRelativeMousePoint(clientX, clientY).Subtract(5.0));
        _ongoingLink = Diagram.Options.Links.Factory(Diagram, port, _targetPositionAnchor);

        if (_ongoingLink is not null)
        {
            Diagram.Links.Add(_ongoingLink);
        }
    }
}
