using System.Collections.Generic;
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
        var blockNodeConnectorSet = new HashSet<BlockNodeConnector>(connectors);
        for (var i = 0; i < Connectors.Count; i++)
        {
            for (var j = 0; j < 2; j++)
            {
                if (Connectors[i][j] is not null && blockNodeConnectorSet.Contains(Connectors[i][j]!))
                {
                    datastore.RemoveMapping(Connectors[i][j]!);
                    Connectors[i][j] = null;
                }
            }
        }

        while (Connectors.Count > BlockNodeLayout.MinimumConnectorRows)
        {
            var lastRow = Connectors[^1];
            if (lastRow[0] is not null || lastRow[1] is not null)
                break;
            Connectors.RemoveAt(Connectors.Count - 1);
        }

        foreach (var con in blockNodeConnectorSet)
            RemovePort(con);

        UpdateSize();
    }

    internal void SetChildrenInformation(int nextLevelElements, int allChildrenElements)
        => ChildrenDisplayText = nextLevelElements == allChildrenElements
            ? $"{nextLevelElements}"
            : $"{nextLevelElements} ({allChildrenElements})";
}
