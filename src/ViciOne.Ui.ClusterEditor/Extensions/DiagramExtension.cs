using System.Collections.Generic;
using System.Linq;
using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Extensions;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Extensions;

internal static class DiagramExtension
{
    public static bool AreAllNodesInViewport(this Diagram diagram, IEnumerable<NodeModel> nodes)
        => nodes.All(diagram.IsNodeInViewport);

    public static bool ArePartialVisibleNodesInViewport(this Diagram diagram, IEnumerable<NodeModel> nodes)
    {
        var viewPort = diagram.GetViewport();
        return nodes.Any(n => viewPort.Intersects(n.GetBounds() ?? Rectangle.Zero));
    }

    public static Rectangle GetNodeBounds(this Diagram diagram)
        => diagram.Nodes.GetBounds();

    public static Rectangle GetNodeBoundsWithFlags(this Diagram _, IEnumerable<NodeModel> nodes, double margin, double flagMargin)
    {
        var nodeBounds = nodes.GetBounds();
        return new(
            nodeBounds.Left - flagMargin - margin,
            nodeBounds.Top - margin,
            nodeBounds.Right + flagMargin + margin,
            nodeBounds.Bottom + margin
        );
    }

    public static (double deltaX, double deltaY) GetNodeViewportDistance(this Diagram diagram)
    {
        var viewport = diagram.GetViewport();
        var nodeRect = diagram.GetNodeBounds();

        return (
            viewport.Center.X - nodeRect.Center.X,
            viewport.Center.Y - nodeRect.Center.Y
        );
    }

    public static Point GetRelativeGridPoint(this Diagram diagram, Point clientPoint, IDatastore datastore)
    {
        var relativePoint = diagram.GetRelativeMousePoint(clientPoint.X, clientPoint.Y);
        var gridPoint = datastore.Builder.GetGridPoint(new((int)relativePoint.X, (int)relativePoint.Y));
        return new(gridPoint.X, gridPoint.Y);
    }

    public static Rectangle GetViewport(this Diagram diagram)
    {
        if (diagram.Container is null)
            return Rectangle.Zero;

        if (!double.IsFinite(diagram.Pan.X) || !double.IsFinite(diagram.Pan.Y))
            diagram.SetPan(0, 0);

        var upperLeftPoint = diagram.GetRelativeMousePoint(diagram.Container.Left, diagram.Container.Top);
        var lowerRightPoint = diagram.GetRelativeMousePoint(diagram.Container.Right, diagram.Container.Bottom);

        return new(
            upperLeftPoint.X, upperLeftPoint.Y,
            lowerRightPoint.X, lowerRightPoint.Y
        );
    }

    public static bool IsNodeInViewport(this Diagram diagram, NodeModel node)
    {
        var viewport = diagram.GetViewport();
        var nodeSize = node.Size ?? Size.Zero;

        return node.Position.X >= viewport.Left &&
            node.Position.Y >= viewport.Top &&
            node.Position.X + nodeSize.Width <= viewport.Right &&
            node.Position.Y + nodeSize.Height <= viewport.Bottom;
    }

    public static void PanToNode(this Diagram diagram, NodeModel node)
    {
        var container = diagram.Container ?? Rectangle.Zero;
        var nodeSize = node.Size ?? Size.Zero;

        diagram.SetPan(
            -((node.Position.X + (nodeSize.Width / 2)) * diagram.Zoom) + (container.Width / 2),
            -((node.Position.Y + (nodeSize.Height / 2)) * diagram.Zoom) + (container.Height / 2));
    }
}
