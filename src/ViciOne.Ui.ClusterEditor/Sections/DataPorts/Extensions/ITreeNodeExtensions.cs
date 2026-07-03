using System.Collections.Generic;
using System.Linq;
using ViciOne.TreeBuilder;
using ViciOne.TreeBuilder.NodeTypes;
using ViciOne.Ui.ClusterEditor.Models.Comparer;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;

internal static class ITreeNodeExtensions
{
    private static int CompareDescriptors(DataPortChildNodeContextMenuDescriptor a, DataPortChildNodeContextMenuDescriptor b)
    {
        var groupComparison = GetSortGroup(a).CompareTo(GetSortGroup(b));
        return groupComparison != 0 ? groupComparison : AlphaNumericComparer.Default.Compare(a.Name, b.Name);
    }

    public static List<DataPortChildNodeContextMenuDescriptor> GetPossibleChildNodes(this ITreeNode node)
    {
        if (node is not DataPortNodeModel dataPortNode)
            return [];

        var rootNode = dataPortNode.GetRootNode();
        var possibleSuccessors = rootNode.Builder.GetPossibleSuccessors(rootNode, dataPortNode);

        var rootDescriptors = new Dictionary<string, DataPortChildNodeContextMenuDescriptor>();
        var namespaceDescriptorMap = new Dictionary<string, DataPortChildNodeContextMenuDescriptor>();
        var rootNamespaceDescriptors = new List<DataPortChildNodeContextMenuDescriptor>();

        foreach (var successorRef in possibleSuccessors)
        {
            var nodeType = rootNode.Builder.NodeTypes[successorRef.Id];

            var leafDescriptor = new DataPortChildNodeContextMenuDescriptor
            {
                IconName = nodeType.Icons.FirstOrDefault() ?? string.Empty,
                IsDataPoint = nodeType.IsDataPoint(),
                Name = nodeType.Name,
                NodeReference = successorRef,
                ParentNode = dataPortNode,
            };

            var namespaceParts = nodeType.Namespace is { Length: > 0 } ns
                ? ns.Split('.')
                : [];

            if (namespaceParts.Length == 0)
            {
                rootDescriptors[nodeType.Name] = leafDescriptor;
                continue;
            }

            var parentPath = string.Empty;

            for (var i = 0; i < namespaceParts.Length; i++)
            {
                var currentPath = i == 0
                    ? namespaceParts[0]
                    : $"{parentPath}.{namespaceParts[i]}";

                if (!namespaceDescriptorMap.ContainsKey(currentPath))
                {
                    var nsDescriptor = new DataPortChildNodeContextMenuDescriptor
                    {
                        IconName = string.Empty,
                        Name = namespaceParts[i],
                        NodeReference = new NodeReference { Id = currentPath },
                        ParentNode = dataPortNode,
                    };

                    namespaceDescriptorMap[currentPath] = nsDescriptor;

                    if (i == 0)
                        rootNamespaceDescriptors.Add(nsDescriptor);
                    else
                        namespaceDescriptorMap[parentPath].Children.Add(nsDescriptor);
                }

                parentPath = currentPath;
            }

            namespaceDescriptorMap[parentPath].Children.Add(leafDescriptor);
        }

        // Sort children at every namespace level
        foreach (var nsDescriptor in namespaceDescriptorMap.Values)
            nsDescriptor.Children.Sort(CompareDescriptors);

        // Add root namespace descriptors, sort and return the root level
        List<DataPortChildNodeContextMenuDescriptor> result = [.. rootDescriptors.Values];
        result.AddRange(rootNamespaceDescriptors);
        result.Sort(CompareDescriptors);
        return result;
    }

    private static int GetSortGroup(DataPortChildNodeContextMenuDescriptor d)
    {
        if (d.Children.Count > 0) return 0;
        if (!d.IsDataPoint) return 1;
        return 2;
    }
}
