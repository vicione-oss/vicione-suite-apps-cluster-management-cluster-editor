using System.Collections.Generic;
using System.Linq;
using ViciOne.TreeBuilder;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;

internal static class ITreeNodeExtensions
{
    public static IEnumerable<DataPortChildNodeModel> GetPossibleChildNodes(this ITreeNode node)
    {
        if (node is not DataPortNodeModel dataPortNode)
            yield break;

        var rootNode = dataPortNode.GetRootNode();
        var possibleSuccessors = rootNode.Builder.GetPossibleSuccessors(rootNode, dataPortNode);

        foreach (var successorRef in possibleSuccessors)
        {
            var nodeType = rootNode.Builder.NodeTypes[successorRef.Id];
            var successorNode = new DataPortChildNodeModel
            {
                AvailableIcons = nodeType.Icons,
                CanHaveChildren = nodeType.ChildNodes.Length != 0,
                DisplayText = nodeType.Name,
                DisplayTextIsReadOnly = nodeType.NameIsReadOnly,
                Icon = nodeType.Icons.FirstOrDefault(),
                IsDataPoint = nodeType.IsDataPoint(),
                NodeReference = successorRef,
                Parent = dataPortNode,
                Properties = [.. rootNode.Builder.GetProperties(nodeType)],
                RootNode = rootNode,
                TransferDirections = nodeType.GetInheritedTransferDirections(dataPortNode),
            };
            yield return successorNode;
        }
    }
}
