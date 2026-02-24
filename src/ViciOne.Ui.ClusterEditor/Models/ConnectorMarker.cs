using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Models;

namespace ViciOne.Ui.ClusterEditor.Models;

public sealed class ConnectorMarker(BlockNodeConnector connector) : IDiagramModel
{
    public BlockNodeConnector Connector { get; init; } = connector;
    public List<Link> Links { get; } = [];
    public bool Selected { get; set; }
    public IEnumerable<PublishedConnectorTooltipEntry> TooltipEntries { get; set; } = [];
    public bool Traced { get; set; }
    public bool Visible { get; set; }
}
