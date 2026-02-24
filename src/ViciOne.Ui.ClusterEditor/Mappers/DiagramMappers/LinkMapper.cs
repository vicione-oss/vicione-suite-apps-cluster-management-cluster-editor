using System;
using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;

internal static class LinkMapper
{
    internal static BlockNodeLink CreateLink(Datastore datastore, Link link)
    {
        if (link.SourceConnector is null)
            throw new InvalidOperationException("Link missing source connector");

        if (link.DestinationConnector is null)
            throw new InvalidOperationException("Link missing destination connector");

        var sourceNodeConnector = datastore.DataflowDiagramMapping.GetDiagramModel(link.SourceConnector);
        var targetNodeConnector = datastore.DataflowDiagramMapping.GetDiagramModel(link.DestinationConnector);

        var nodeLink = new BlockNodeLink(sourceNodeConnector, targetNodeConnector)
        {
            DrawOverlay = true
        };
        nodeLink.UpdateLineStyle();

        return nodeLink;
    }

    internal static void ReloadLinks(Datastore datastore, DiagramService diagramService, IEnumerable<Link> links)
    {
        diagramService.Diagram.Links.Remove(datastore.DataflowDiagramMapping.GetDiagramModels(links));

        var newLinks = new List<BlockNodeLink>();
        foreach (var link in links)
        {
            datastore.DataflowDiagramMapping.Remove(link);

            var linkNode = CreateLink(datastore, link);
            newLinks.Add(linkNode);
            datastore.DataflowDiagramMapping.Add(link, linkNode);
        }

        diagramService.Diagram.Links.Add(newLinks);
    }
}
