using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.TreeEditor.Builder;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class DataPortTreeState
{
    private readonly List<DataPortRootNodeModel> _dataPorts = [];

    public ITreeBuilder Builder { get; set; } = default!;
    public DataPortNodeModel? EditingTreeNode { get; set; }
    public bool IsDeletionInProgress { get; set; }
    public Action<ITreeNode, Action>? OnDeleteNodeUserConfirmationRequest { get; set; }

    /// <summary>
    /// Values by property name that were changed but not saved yet, shown by the edit form of <see cref="EditingTreeNode"/> in place of the node's own values.
    /// </summary>
    public IReadOnlyDictionary<string, object?> PendingEditValues { get; set; } = ReadOnlyDictionary<string, object?>.Empty;
    public IReadOnlyList<DataPortRootNodeModel> RootNodes => _dataPorts;
    public List<ITreeNode> ValidInboundDropTargets { get; } = [];

    public void AddRootNode(DataPortRootNodeModel node)
        => _dataPorts.Add(node);

    public void ClearRootNodes()
        => _dataPorts.Clear();

    /// <summary>
    /// The node with <paramref name="id"/> anywhere in the tree, root nodes included, or
    /// <see langword="null"/> when no root node still reaches it.
    /// </summary>
    public DataPortNodeModel? FindAnyNode(GuidNodeIdentifier id)
    {
        foreach (var rootNode in _dataPorts)
        {
            var treeNode = FindTreeNode(rootNode, id);
            if (treeNode is not null)
                return treeNode;
        }

        return null;
    }

    public DataPortChildNodeModel? FindNode(Guid nodeId)
        => _dataPorts.FindNode(nodeId);

    public DataPortRootNodeModel? FindRootNode(Func<DataPortRootNodeModel, bool> predicate)
        => _dataPorts.FirstOrDefault(predicate);

    public static DataPortNodeModel? FindTreeNode(DataPortNodeModel parentNode, GuidNodeIdentifier id)
    {
        if (parentNode.Id == id)
            return parentNode;

        foreach (var node in parentNode.Children)
        {
            var treeNode = FindTreeNode(node, id);
            if (treeNode is not null)
                return treeNode;
        }

        return null;
    }

    public DataPortRootNodeModel? GetDataPortRootNode(string rulesetRootId)
        => _dataPorts.FirstOrDefault(k => k.Builder.Ruleset.Root?.Id.Equals(rulesetRootId, StringComparison.OrdinalIgnoreCase) ?? false);

    public IEnumerable<ITreeNode> GetSelectedNodes()
    {
        var result = new List<ITreeNode>();
        result.AddRange(_dataPorts.Where(dprn => dprn.Selected));

        foreach (var dataPortNode in _dataPorts.SelectMany(dprn => dprn.Children).ToArray())
            result.AddRange(GetSelectedNodesRecursive(dataPortNode));

        return result;

        static IEnumerable<DataPortNodeModel> GetSelectedNodesRecursive(DataPortNodeModel node)
        {
            var result = new List<DataPortNodeModel>();
            if (node.Selected)
                result.Add(node);

            foreach (var child in node.Children)
                result.AddRange(GetSelectedNodesRecursive(child));

            return result;
        }
    }

    public ITreeNode? GetTreeNode(DataPortTreeNode dataPortTreeNode)
    {
        DataPortNodeModel? treeNode = null;
        foreach (var rootNode in _dataPorts)
        {
            treeNode = FindTreeNode(rootNode, new GuidNodeIdentifier(dataPortTreeNode.Id));
            if (treeNode is not null)
                break;
        }

        return treeNode;
    }

    public bool RemoveRootNode(DataPortRootNodeModel node)
        => _dataPorts.Remove(node);
}
