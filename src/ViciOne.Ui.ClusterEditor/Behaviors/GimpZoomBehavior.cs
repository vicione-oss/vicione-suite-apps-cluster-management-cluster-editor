using System;
using Blazor.Diagrams.Core;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Behaviors;

// This code is inspired by Blazor.Diagrams.Core.Behaviors.ZoomBehavior
internal sealed class GimpZoomBehavior : Behavior
{
    private readonly DiagramEventService _diagramEventService;

    public GimpZoomBehavior(Diagram diagram, DiagramEventService diagramEventService) : base(diagram)
    {
        _diagramEventService = diagramEventService;
        Diagram.Wheel += OnWheel;
        _diagramEventService.EdgeDraggingWheel += OnEdgeDraggingWheel;
    }

    public override void Dispose()
    {
        Diagram.Wheel -= OnWheel;
        _diagramEventService.EdgeDraggingWheel -= OnEdgeDraggingWheel;
        GC.SuppressFinalize(this);
    }

    private void OnEdgeDraggingWheel(WheelEventArgs e)
    {
        if (!e.AltKey)
            return;

        Wheel(e.DeltaY, e.ClientX, e.ClientY);
    }

    private void OnWheel(global::Blazor.Diagrams.Core.Events.WheelEventArgs e)
    {
        if (!e.AltKey)
            return;

        Wheel(e.DeltaY, e.ClientX, e.ClientY);
    }

    private void Wheel(double deltaY, double clientX, double clientY)
    {
        if (Diagram.Container is null ||
            deltaY == 0 ||
            !Diagram.Options.Zoom.Enabled)
        {
            return;
        }

        var scaleFactor = ((Diagram.Options.Zoom.ScaleFactor - 1) * (Math.Abs(deltaY) / 100)) + 1;
        var scale = Math.Clamp(scaleFactor, 1.01, 2);
        var oldZoom = Diagram.Zoom;
        var dY = Diagram.Options.Zoom.Inverse ? deltaY * -1 : deltaY;
        var newZoom = dY > 0 ? oldZoom * scale : oldZoom / scale;
        newZoom = Math.Clamp(newZoom, Diagram.Options.Zoom.Minimum, Diagram.Options.Zoom.Maximum);

        if (newZoom < 0 || newZoom == Diagram.Zoom)
            return;

        var clientWidth = Diagram.Container!.Width;
        var clientHeight = Diagram.Container.Height;
        var widthDiff = (clientWidth * newZoom) - (clientWidth * oldZoom);
        var heightDiff = (clientHeight * newZoom) - (clientHeight * oldZoom);
        var relativeX = clientX - Diagram.Container.Left;
        var relativeY = clientY - Diagram.Container.Top;
        var xFactor = (relativeX - Diagram.Pan.X) / oldZoom / clientWidth;
        var yFactor = (relativeY - Diagram.Pan.Y) / oldZoom / clientHeight;
        var newPanX = Diagram.Pan.X - (widthDiff * xFactor);
        var newPanY = Diagram.Pan.Y - (heightDiff * yFactor);

        Diagram.Batch(() =>
        {
            Diagram.SetPan(newPanX, newPanY);
            Diagram.SetZoom(newZoom);
        });
    }
}
