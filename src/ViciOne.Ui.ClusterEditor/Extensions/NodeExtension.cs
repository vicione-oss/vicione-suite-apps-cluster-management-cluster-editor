using System;
using System.Collections.Generic;
using System.Linq;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Extensions;

internal static class NodeExtension
{
    public static void AlignNodes(this IEnumerable<NodeModel> nodes, Alignment align)
    {
        var nodesArray = nodes.ToArray();
        var positionValue = align switch
        {
            Alignment.Bottom => nodesArray.First().Position.Y + nodesArray.First().Size!.Height,
            Alignment.Left => nodesArray.First().Position.X,
            Alignment.Right => nodesArray.First().Position.X + nodesArray.First().Size!.Width,
            Alignment.Top or Alignment.TopByLowestElement => nodesArray.First().Position.Y,
            _ => throw new ArgumentException($"Unknown {nameof(Alignment)} value.")
        };

        foreach (var node in nodesArray.Skip(1))
        {
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

        foreach (var node in nodesArray)
        {
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
