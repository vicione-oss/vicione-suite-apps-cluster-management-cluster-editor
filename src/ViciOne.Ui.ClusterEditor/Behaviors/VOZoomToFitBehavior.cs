using System;
using System.Collections.Generic;
using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Events;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Extensions;

namespace ViciOne.Ui.ClusterEditor.Behaviors;

internal sealed class VOZoomToFitBehavior : Behavior
{
    private bool _isSelfUpdating;
    private bool _isZoomedToFit;
    private Point? _previousPan;
    private double _previousZoom;

    public VOZoomToFitBehavior(Diagram diagram) : base(diagram)
    {
        Diagram.KeyDown += OnDiagramKeyDown;
        Diagram.PanChanged += OnPanChanged;
        Diagram.ZoomChanged += OnZoomChanged;
    }

    public override void Dispose()
    {
        Diagram.KeyDown -= OnDiagramKeyDown;
        Diagram.PanChanged -= OnPanChanged;
        Diagram.ZoomChanged -= OnZoomChanged;

        GC.SuppressFinalize(this);
    }

    private void OnDiagramKeyDown(KeyboardEventArgs e)
    {
        if (e.Code != "Space")
            return;

        if (_isZoomedToFit)
            ZoomToPrevious();
        else
            ZoomToFit();
    }

    private void OnPanChanged()
    {
        if (_isSelfUpdating)
            return;

        _isZoomedToFit = false;
    }

    private void OnZoomChanged()
    {
        if (_isSelfUpdating)
            return;

        _isZoomedToFit = false;
    }

    public void ZoomToFit()
    {
        if (Diagram.Nodes.Count == 0)
            return;

        _isSelfUpdating = true;

        _previousPan = new(Diagram.Pan.X, Diagram.Pan.Y);
        _previousZoom = Diagram.Zoom;

        // Der nachstehende Code ist abgewandelt aus der Diagram.ZoomToFit Methode entnommen.
        Diagram.Batch(() =>
        {
            var container = Diagram!.Container;
            if (container is null)
                return;

            var selectedNodes = new List<NodeModel>();
            foreach (var node in Diagram.Nodes)
            {
                if (node.Selected)
                    selectedNodes.Add(node);
            }

            var bounds = Diagram.GetNodeBoundsWithFlags(
                selectedNodes.Count > 0 ? selectedNodes : Diagram.Nodes,
                DiagramSettings.DefaultZoomToFitMargin,
                DiagramSettings.DefaultGridSize * DiagramSettings.FlagMarginGridCells
            );

            var xf = container.Width / bounds.Width;
            var yf = container.Height / bounds.Height;

            var zoomFactor = Math.Clamp(Math.Min(xf, yf), DiagramSettings.ZoomMinimum, DiagramSettings.ZoomMaximum);
            Diagram.SetZoom(zoomFactor);

            var centerX = (container.Width - (bounds.Width * Diagram.Zoom)) / 2;
            var centerY = (container.Height - (bounds.Height * Diagram.Zoom)) / 2;
            var nx = container.Left + Diagram.Pan.X + (bounds.Left * Diagram.Zoom) - centerX;
            var ny = container.Top + Diagram.Pan.Y + (bounds.Top * Diagram.Zoom) - centerY;
            Diagram.UpdatePan(container.Left - nx, container.Top - ny);
        });

        _isZoomedToFit = true;
        _isSelfUpdating = false;
    }

    public void ZoomToPrevious()
    {
        _isSelfUpdating = true;

        Diagram.Batch(() =>
        {
            Diagram.SetZoom(_previousZoom);
            Diagram.UpdatePan(_previousPan!.X - Diagram.Pan.X, _previousPan.Y - Diagram.Pan.Y);
        });

        _isZoomedToFit = false;
        _isSelfUpdating = false;
    }
}
