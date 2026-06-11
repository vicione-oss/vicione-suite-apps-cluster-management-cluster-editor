using System;
using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Core.Contracts;
using ViciOne.Core.Dataflow.DataModel;

using Connector = ViciOne.Cluster.Model.Connector;

namespace ViciOne.Ui.ClusterEditor.Models;

public sealed class DataGridConnectorWrapper(IConnector connector,
    ConnectorDesign connectorDesign,
    FunctionBlockDesign functionBlockDesign,
    ConnectorMarkerType destinationMarker = ConnectorMarkerType.None) : IDragable
{
    public IConnector Connector { get; } = connector;
    public string ConnectorName => Connector.Name;
    public Type ConnectorType => connectorDesign.ConnectorType;
    public string ConnectorTypeName => DataTypeCompatibilityValidator.DetermineValueType(ConnectorType).Name;
    public string Description => Connector.Description ?? string.Empty;
    public string DesignName => functionBlockDesign.Name;
    public ConnectorMarkerType DestinationMarker { get; } = destinationMarker;
    public Guid FunctionBlockId => Connector.FunctionBlock.Id;
    public string FunctionBlockName => Connector.FunctionBlock.Name;
    public bool IsInput => Connector is IConnectorInput;

    public int Links
    {
        get
        {
            var count = 0;
            foreach (var link in Connector.Links)
            {
                if (!link.Visible)
                    count++;
            }
            return count;
        }
    }

    public string ParentName => ((INamedContainerChild)Connector.Parent).Name;

    public string Path
    {
        get
        {
            var containers = ContainerChildExtensions.GetAllUpstreamContainers(Connector.FunctionBlock);
            var parts = new List<string>();
            foreach (var c in containers)
                parts.Add(c.Name);
            parts.Reverse();
            return string.Join('.', parts);
        }
    }
}
