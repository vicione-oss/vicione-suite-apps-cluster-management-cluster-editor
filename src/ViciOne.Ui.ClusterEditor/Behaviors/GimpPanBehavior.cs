using System;
using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models.Base;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

using DiagramEvents = Blazor.Diagrams.Core.Events;

namespace ViciOne.Ui.ClusterEditor.Behaviors;

// This code is inspired by Blazor.Diagrams.Core.Behaviors.PanBehavior
internal sealed class GimpPanBehavior : Behavior, IPanBehavior
{
    private readonly DiagramEventService _diagramEventService;
    private bool _eventsSubscribed;
    private double _initialClientX;
    private double _initialClientY;
    private Point? _initialPan;

    public GimpPanBehavior(Diagram diagram, DiagramEventService diagramEventService) : base(diagram)
    {
        _diagramEventService = diagramEventService;

        Diagram.PointerDown += OnPointerDown;
        Diagram.Wheel += OnWheel;
        _diagramEventService.EdgeDraggingWheel += OnEdgeDraggingWheel;
    }

    public override void Dispose()
    {
        Diagram.PointerDown -= OnPointerDown;
        Diagram.Wheel -= OnWheel;
        _diagramEventService.EdgeDraggingWheel -= OnEdgeDraggingWheel;

        GC.SuppressFinalize(this);
    }

    private void End()
    {
        if (!Diagram.Options.AllowPanning)
            return;

        if (_eventsSubscribed)
        {
            Diagram.PointerMove -= OnPointerMove;
            Diagram.PointerUp -= OnPointerUp;
            _diagramEventService.EdgeDraggingPointerMove -= OnEdgeDraggingPointerMove;
            _diagramEventService.EdgeDraggingPointerUp -= OnEdgeDraggingPointerUp;

            _eventsSubscribed = false;
        }

        _initialPan = null;
    }

    private void Move(double clientX, double clientY)
    {
        if (!Diagram.Options.AllowPanning || _initialPan is null)
            return;

        var deltaX = clientX - _initialClientX - (Diagram.Pan.X - _initialPan.X);
        var deltaY = clientY - _initialClientY - (Diagram.Pan.Y - _initialPan.Y);
        Diagram.UpdatePan(deltaX, deltaY);
    }

    private void OnEdgeDraggingPointerMove(MouseEventArgs e)
        => Move(e.ClientX, e.ClientY);

    private void OnEdgeDraggingPointerUp(MouseEventArgs _)
        => End();

    private void OnEdgeDraggingWheel(WheelEventArgs e)
    {
        if (e.AltKey)
            return;

        Wheel(e.DeltaY, e.ShiftKey);
    }

    private void OnPointerDown(Model? _, DiagramEvents.PointerEventArgs e)
    {
        if (e.ShiftKey || e.Button != (int)MouseEventButton.Wheel)
            return;

        Start(e.ClientX, e.ClientY);
    }

    private void OnPointerMove(Model? _, DiagramEvents.PointerEventArgs e)
        => Move(e.ClientX, e.ClientY);

    private void OnPointerUp(Model? _, DiagramEvents.PointerEventArgs _2)
        => End();

    private void OnWheel(DiagramEvents.WheelEventArgs e)
    {
        if (e.AltKey)
            return;

        Wheel(e.DeltaY, e.ShiftKey);
    }

    private void Start(double clientX, double clientY)
    {
        if (!Diagram.Options.AllowPanning)
            return;

        if (!_eventsSubscribed)
        {
            Diagram.PointerMove += OnPointerMove;
            Diagram.PointerUp += OnPointerUp;
            _diagramEventService.EdgeDraggingPointerMove += OnEdgeDraggingPointerMove;
            _diagramEventService.EdgeDraggingPointerUp += OnEdgeDraggingPointerUp;

            _eventsSubscribed = true;
        }

        _initialPan = Diagram.Pan;
        _initialClientX = clientX;
        _initialClientY = clientY;
    }

    public void StopPointerMove()
        => End();

    private void Wheel(double deltaY, bool horizontal)
    {
        if (!Diagram.Options.AllowPanning)
            return;

        var delta = deltaY * -0.5;

        if (horizontal)
            Diagram.UpdatePan(delta, 0);
        else
            Diagram.UpdatePan(0, delta);
    }
}
