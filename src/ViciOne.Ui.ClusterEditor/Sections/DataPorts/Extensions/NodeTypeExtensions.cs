using System.Collections.Generic;
using ViciOne.TreeBuilder.NodeTypes;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;

internal static class NodeTypeExtensions
{
    internal static IList<DataPortTransferDirection> GetInheritedTransferDirections(this NodeType nodeType, DataPortNodeModel parentNode)
    {
        if (nodeType.TransferDirections.Length > 0)
            return nodeType.TransferDirections;
        if (parentNode is DataPortChildNodeModel parentChildNode)
            return parentChildNode.TransferDirections;
        return [];
    }

    internal static bool IsDataPoint(this NodeType nodeType)
        => nodeType.DataTypes.Length > 0;
}
