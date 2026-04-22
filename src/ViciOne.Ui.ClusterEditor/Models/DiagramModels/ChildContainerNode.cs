using System;
using System.Collections.Generic;
using System.Linq;
using Blazor.Diagrams.Core.Geometry;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Models.DiagramModels;

public sealed class ChildContainerNode : BlockNode
{
    internal string ChildrenDisplayText { get; set; } = string.Empty;

    internal ChildContainerNode(Point? point = null) : base(point) { }

    internal void RemoveConnectors(IEnumerable<BlockNodeConnector> connectors, IDatastore datastore)
    {
        var blockNodeConnectors = connectors.ToArray();
        for (var i = 0; i < Connectors.Count; i++)
        {
            for (var j = 0; j < 2; j++)
            {
                if (blockNodeConnectors.Contains(Connectors[i][j]))
                {
                    datastore.RemoveMapping(Connectors[i][j]!);
                    Connectors[i][j] = null;
                }
            }
        }

        while (Connectors.Count > BlockNodeLayout.MinimumConnectorRows && Connectors.Last().All(c => c is null))
            Connectors.RemoveAt(Connectors.Count - 1);

        UpdateSize();

        foreach (var con in blockNodeConnectors)
            RemovePort(con);

        ReinitializePorts();
    }

    internal void SetChildrenInformation(int nextLevelElements, int allChildrenElements)
        => ChildrenDisplayText = nextLevelElements == allChildrenElements
            ? $"{nextLevelElements}"
            : $"{nextLevelElements} ({allChildrenElements})";
}
