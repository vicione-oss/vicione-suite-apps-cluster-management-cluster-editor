using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;

internal static class DataPortTreeNodeExtensions
{
    public static void ConvertToEditorNode(this DataPortTreeNode clusterTreeNode, DataPortRootNodeModel rootNode, DataPortNodeModel editorParent)
    {

        var childNodeReferences = editorParent.NodeReference switch
        {
            null => throw new InvalidOperationException($"No NodeReference found for {editorParent.Id.Value}"),
            _ => rootNode.Builder.NodeTypes.TryGetValue(editorParent.NodeReference.Id, out var rootNodeType)
                ? rootNodeType.ChildNodes
                : throw new InvalidOperationException($"NodeReference with Id {editorParent.NodeReference.Id} is not part of the current ruleset")
        };

        var nodeRef = childNodeReferences.FirstOrDefault(c => c.Id == clusterTreeNode.DesignId)
            ?? throw new InvalidOperationException($"Node reference for DesignId={clusterTreeNode.DesignId} not found!");

        var nodeType = rootNode.Builder.NodeTypes[nodeRef.Id];

        var newChild = new DataPortChildNodeModel
        {
            AvailableIcons = nodeType.Icons,
            CanHaveChildren = nodeType.ChildNodes.Length != 0,
            DisplayText = clusterTreeNode.Name,
            DisplayTextIsReadOnly = nodeType.NameIsReadOnly,
            Icon = clusterTreeNode.Icon ?? nodeType.Icons.FirstOrDefault(),
            Id = new GuidNodeIdentifier(clusterTreeNode.Id),
            IsDataPoint = nodeType.IsDataPoint(),
            NodeReference = nodeRef,
            Parent = editorParent,
            Properties = [.. rootNode.Builder.GetProperties(nodeType)],
            RootNode = rootNode,
            TransferDirections = nodeType.GetInheritedTransferDirections(editorParent),
        };

        editorParent.Children.Add(newChild);

        foreach (var treeNode in clusterTreeNode.Children)
            treeNode.ConvertToEditorNode(rootNode, newChild);

        newChild.AssignValuesAndProperties(clusterTreeNode, rootNode.Builder);
    }

    public static string GetPath(this DataPortTreeNode dataPortTreeNode, Datastore datastore)
    {
        List<string> pathParts = [];
        IHasDataPortTreeNodes hasTreeNodes = dataPortTreeNode;
        do
        {
            hasTreeNodes = datastore.Builder.Cache.GetDataPortTreeNodeParent((DataPortTreeNode)hasTreeNodes);
            pathParts.Add(GetName(hasTreeNodes));
        }
        while (hasTreeNodes is not DataPort);

        pathParts.Reverse();
        return string.Join(".", [.. pathParts]);

        static string GetName(IHasDataPortTreeNodes hasDataPortTreeNodes)
            => hasDataPortTreeNodes switch
            {
                DataPortTreeNode node => node.Name,
                DataPort dataPort => dataPort.Name,
                _ => throw new NotImplementedException(),
            };
    }

    public static IEnumerable<BlockNodeConnector> GetValidTargetConnectors(this DataPortTreeNode dataPortTreeNode, Datastore datastore)
    {
        var targetConnectors = datastore.DataflowDiagramMapping.GetConnectors()
            .Where(c => datastore.Builder.Editors.DataPortTreeNode.CanAssignConnector(dataPortTreeNode, c is ContainerConnector ? c.GetUnderlyingConnector() : (Connector)c));

        return [.. targetConnectors.Select(datastore.DataflowDiagramMapping.GetDiagramModel)];
    }
}
