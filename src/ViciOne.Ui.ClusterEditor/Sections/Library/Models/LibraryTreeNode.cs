using System.Collections.Generic;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;

namespace ViciOne.Ui.ClusterEditor.Sections.Library.Models;

internal sealed class LibraryTreeNode : IClusterEditorTreeNode
{
    public IEnumerable<LibraryTreeNode> Children { get; set; } = [];
    public required string DisplayText { get; set; }
    public bool Expanded { get; set; }
    public required INodeIdentifier Id { get; set; }
    public required LibraryEntry LibraryEntry { get; set; }
    public LibraryTreeNode? Parent { get; set; }
    public bool Selected { get; set; }
}
