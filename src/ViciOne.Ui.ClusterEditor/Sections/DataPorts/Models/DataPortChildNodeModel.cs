using System.Collections.Generic;
using ViciOne.TreeBuilder.NodeTypes;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

public sealed class DataPortChildNodeModel : DataPortNodeModel
{
    public bool IsDataPoint { get; set; }
    public required DataPortNodeModel Parent { get; set; }
    public required IList<IDataPortNodeModelProperty> Properties { get; init; }
    public required DataPortRootNodeModel RootNode { get; set; }
    public required IList<DataPortTransferDirection> TransferDirections { get; init; }
}
