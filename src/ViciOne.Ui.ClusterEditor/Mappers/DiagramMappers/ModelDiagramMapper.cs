using System.Collections.Generic;
using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Models;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;

internal static class ModelDiagramMapper
{
    internal static void AddToDiagram(Diagram diagram, IEnumerable<NodeModel> nodes, IEnumerable<LinkModel> links)
    {
        diagram.Batch(() =>
        {
            diagram.Nodes.Add(nodes);
            diagram.Links.Add(links);
        });
        diagram.RefreshOrders(refresh: false);
    }

    internal static void RemoveNodesFromDiagram(DiagramService diagramService, IEnumerable<NodeModel> nodes)
    {
        diagramService.DiagramState.SuppressEvents = true;
        diagramService.Diagram.Nodes.Remove(nodes);
        diagramService.DiagramState.SuppressEvents = false;
    }
}
