using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Ui.ClusterEditor.Sections.Library.Models;
using ViciOne.Ui.TreeEditor.Builder.Interface;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace ViciOne.Ui.ClusterEditor.Sections.Library.Services;

internal sealed class LibraryTreeAdapter : TreeAdapter
{
    private readonly List<LibraryTreeNode> _nodes = [];

    public event Action<LibraryTreeNode>? DblClick;

    public override bool CanSelectNode(ITreeNode node, IEnumerable<ITreeNode> currentSelection, bool willDeselectOthers)
    {
        if (node is LibraryTreeNode tNode)
            return !tNode.LibraryEntry.IsStructureNode;

        return false;
    }

    public override void Dismantle()
    {
        Builder.Expansion.ExpansionChanged -= OnExpansionChanged;
        Builder.Selection.SelectionChanged -= OnSelectionChanged;
    }

    public override IEnumerable<ITreeNode> GetChildren(ITreeNode node)
    {
        if (node is not LibraryTreeNode tNode)
            return [];

        return tNode.Children;
    }

    public override Action<ITreeNode>? GetDblClickAction(ITreeNode node)
    {
        if (node is not LibraryTreeNode tNode || tNode.LibraryEntry.IsStructureNode)
            return null;

        return (e) => DblClick?.Invoke((LibraryTreeNode)e);
    }

    public override string GetDisplayText(ITreeNode node)
    {
        if (node is LibraryTreeNode libNode)
            return libNode.DisplayText;

        return string.Empty;
    }

    public override ITreeNode? GetParent(ITreeNode node)
    {
        if (node is not LibraryTreeNode tNode)
            return null;

        return tNode.Parent;
    }

    public override IEnumerable<ITreeNode> GetRootNodes()
        => _nodes;

    public override bool HasChildren(ITreeNode node)
    {
        if (node is not LibraryTreeNode tNode)
            return false;

        return tNode.Children.Any();
    }

    public override bool IsExpanded(ITreeNode treeNode)
    {
        if (treeNode is LibraryTreeNode libNode)
            return libNode.Expanded;

        return false;
    }

    public override bool IsSelected(ITreeNode treeNode)
    {
        if (treeNode is LibraryTreeNode libNode)
            return libNode.Selected;

        return false;
    }

    private void OnExpansionChanged(ITreeNode node, bool expanded)
    {
        if (node is LibraryTreeNode libNode)
            libNode.Expanded = expanded;
    }

    private void OnSelectionChanged(ITreeNode node, bool selected)
    {
        if (node is LibraryTreeNode libNode)
            libNode.Selected = selected;
    }

    public void SetEntries(IEnumerable<LibraryEntry> entries)
    {
        _nodes.Clear();
        foreach (var entry in entries)
        {
            var rootEntry = EntryToNode(entry, null);
            SetEntriesRecursively(rootEntry, entry.Children);
            _nodes.Add(rootEntry);
        }

        Builder.Reset();

        void SetEntriesRecursively(LibraryTreeNode parent, IEnumerable<LibraryEntry> children)
        {
            var resultingChildren = new List<LibraryTreeNode>();
            foreach (var child in children)
            {
                var tNode = EntryToNode(child, parent);
                SetEntriesRecursively(tNode, child.Children);
                resultingChildren.Add(tNode);
            }

            parent.Children = resultingChildren;
        }

        LibraryTreeNode EntryToNode(LibraryEntry entry, LibraryTreeNode? parent)
            => new()
            {
                DisplayText = entry.Name,
                Expanded = entry.IsStructureNode,
                Id = new GuidNodeIdentifier(entry.UniqueId),
                LibraryEntry = entry,
                Parent = parent,
            };
    }

    public override void Setup()
    {
        Builder.Expansion.ExpansionChanged += OnExpansionChanged;
        Builder.Selection.SelectionChanged += OnSelectionChanged;

        Builder.DragAndDrop.EnableOutbound = true;
        Builder.Guidelines.Show = true;
    }
}
