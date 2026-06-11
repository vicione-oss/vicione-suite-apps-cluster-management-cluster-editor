using System;
using System.Collections.Generic;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Extensions;

internal static class NodeExtension
{
    public static void AlignNodes(this IEnumerable<NodeModel> nodes, Alignment align)
    {
        var nodesArray = nodes is IList<NodeModel> list ? list : [.. nodes];
        if (nodesArray.Count == 0)
            return;

        var first = nodesArray[0];
        var positionValue = align switch
        {
            Alignment.Bottom => first.Position.Y + first.Size!.Height,
            Alignment.Left => first.Position.X,
            Alignment.Right => first.Position.X + first.Size!.Width,
            Alignment.Top or Alignment.TopByLowestElement => first.Position.Y,
            _ => throw new ArgumentException($"Unknown {nameof(Alignment)} value.")
        };

        for (var i = 1; i < nodesArray.Count; i++)
        {
            var node = nodesArray[i];
            switch (align)
            {
                case Alignment.Bottom:
                    if (node.Position.Y + node.Size!.Height > positionValue)
                        positionValue = node.Position.Y + node.Size!.Height;
                    break;
                case Alignment.Left:
                    if (node.Position.X < positionValue)
                        positionValue = node.Position.X;
                    break;
                case Alignment.Right:
                    if (node.Position.X + node.Size!.Width > positionValue)
                        positionValue = node.Position.X + node.Size!.Width;
                    break;
                case Alignment.Top:
                    if (node.Position.Y < positionValue)
                        positionValue = node.Position.Y;
                    break;
                case Alignment.TopByLowestElement:
                    if (node.Position.Y > positionValue)
                        positionValue = node.Position.Y;
                    break;
                default:
                    break;
            }
        }

        for (var i = 0; i < nodesArray.Count; i++)
        {
            var node = nodesArray[i];
            switch (align)
            {
                case Alignment.Bottom:
                    node.SetPosition(node.Position.X, positionValue - node.Size!.Height);
                    break;
                case Alignment.Left:
                    node.SetPosition(positionValue, node.Position.Y);
                    break;
                case Alignment.Right:
                    node.SetPosition(positionValue - node.Size!.Width, node.Position.Y);
                    break;
                case Alignment.Top:
                case Alignment.TopByLowestElement:
                    node.SetPosition(node.Position.X, positionValue);
                    break;
                default:
                    break;
            }
        }
    }

    public static Rectangle GetRoundedBounds(this NodeModel node, bool includePorts = false)
    {
        var bounds = node!.GetBounds(includePorts);
        return new Rectangle(
            new Point(Math.Round(bounds!.Left, 0), Math.Round(bounds.Top, 0)),
            new Size(Math.Round(bounds.Width, 0), Math.Round(bounds.Height, 0)));
    }
}
