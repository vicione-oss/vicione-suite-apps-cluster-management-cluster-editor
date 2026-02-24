using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blazor.Diagrams.Core.Geometry;
using Microsoft.JSInterop;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Models.DiagramModels;

public sealed class ChildContainerNode : BlockNode
{
    internal string ChildrenDisplayText { get; set; } = string.Empty;

    internal ChildContainerNode(Point? point = null) : base(point) { }

    internal async Task RemoveConnectorsAsync(IEnumerable<BlockNodeConnector> connectors, Datastore datastore, IJSRuntime jsRuntime, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

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

        cancellationToken.ThrowIfCancellationRequested();

        NameFieldHeight = await jsRuntime.MeasureNameFieldHeightAsync(Name, cancellationToken);
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
