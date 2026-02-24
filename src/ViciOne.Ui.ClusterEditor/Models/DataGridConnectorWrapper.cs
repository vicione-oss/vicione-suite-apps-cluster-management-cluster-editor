using System;
using System.Linq;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Core.Contracts;
using ViciOne.Core.Dataflow.DataModel;

using Connector = ViciOne.Cluster.Model.Connector;

namespace ViciOne.Ui.ClusterEditor.Models;

public sealed class DataGridConnectorWrapper(IConnector connector, ConnectorDesign connectorDesign, FunctionBlockDesign functionBlockDesign) : IDragable
{
    public IConnector Connector { get; } = connector;
    public string ConnectorName => Connector.Name;
    public Type ConnectorType => connectorDesign.ConnectorType;
    public string ConnectorTypeName => DataTypeCompatibilityValidator.DetermineValueType(ConnectorType).Name;
    public string Description => Connector.Description ?? string.Empty;
    public string DesignName => functionBlockDesign.Name;
    public Guid FunctionBlockId => Connector.FunctionBlock.Id;
    public string FunctionBlockName => Connector.FunctionBlock.Name;
    public bool IsInput => Connector is IConnectorInput;
    public int Links => Connector.Links.Count(x => !x.Visible);
    public string ParentName => ((INamedContainerChild)Connector.Parent).Name;
    public string Path => string.Join('.', ContainerChildExtensions
        .GetAllUpstreamContainers(Connector.FunctionBlock)
        .Select(x => x.Name)
        .Reverse());
}
