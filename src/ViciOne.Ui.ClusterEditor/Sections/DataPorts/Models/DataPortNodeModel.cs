using System.Collections.Generic;
using ViciOne.Tree.Builder.NodeTypes;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

public abstract class DataPortNodeModel : IClusterEditorTreeNode, Tree.Builder.ITreeNode, IDragable
{
    public bool CanHaveChildren { get; set; }
    public List<DataPortNodeModel> Children { get; init; } = [];
    IList<Tree.Builder.ITreeNode> Tree.Builder.ITreeNode.Children
        => [.. Children];
    public bool Expanded { get; set; }
    public bool HasChangedProperties { get; set; }
    public bool Highlighted { get; set; }
    public string? Icon { get; set; }
    public GuidNodeIdentifier Id { get; set; } = GuidNodeIdentifier.New();
    INodeIdentifier ITreeNode.Id
        => Id;
    public bool IsDetailViewActive { get; set; }
    public bool IsEditModeActive { get; set; }
    public required string Name { get; set; }
    public bool NameIsReadOnly { get; set; }
    public NodeReference? NodeReference { get; set; }
    public IReadOnlyList<DataPortChildNodeContextMenuDescriptor> PossibleChildren { get; set; } = [];
    public bool Selected { get; set; }
}
