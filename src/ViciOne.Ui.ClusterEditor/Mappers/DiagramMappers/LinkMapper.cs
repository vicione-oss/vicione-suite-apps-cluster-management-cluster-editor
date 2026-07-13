using System;
using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;

internal static class LinkMapper
{
    internal static BlockNodeLink CreateLink(IDatastoreState datastoreState, Link link)
    {
        if (link.SourceConnector is null)
            throw new InvalidOperationException("Link missing source connector");

        if (link.DestinationConnector is null)
            throw new InvalidOperationException("Link missing destination connector");

        var sourceNodeConnector = datastoreState.DataflowDiagramMapping.GetDiagramModel(link.SourceConnector);
        var targetNodeConnector = datastoreState.DataflowDiagramMapping.GetDiagramModel(link.DestinationConnector);

        var nodeLink = new BlockNodeLink(sourceNodeConnector, targetNodeConnector)
        {
            DrawOverlay = true
        };
        nodeLink.UpdateLineStyle();

        return nodeLink;
    }

    internal static void ReloadLinks(IDatastoreState datastoreState, DiagramService diagramService, IEnumerable<Link> links)
    {
        diagramService.Diagram.Links.Remove(datastoreState.DataflowDiagramMapping.GetDiagramModels(links));

        var newLinks = new List<BlockNodeLink>();
        foreach (var link in links)
        {
            datastoreState.DataflowDiagramMapping.Remove(link);

            var linkNode = CreateLink(datastoreState, link);
            newLinks.Add(linkNode);
            datastoreState.DataflowDiagramMapping.Add(link, linkNode);
        }

        diagramService.Diagram.Links.Add(newLinks);
    }
}
