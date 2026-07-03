using System.Collections.Generic;
using ViciOne.TreeBuilder.NodeTypes;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

public abstract class DataPortNodeModel : IClusterEditorTreeNode, TreeBuilder.ITreeNode, IDragable
{
    public IReadOnlyList<string> AvailableIcons { get; set; } = [];
    public bool CanHaveChildren { get; set; }
    public List<DataPortNodeModel> Children { get; init; } = [];
    IList<TreeBuilder.ITreeNode> TreeBuilder.ITreeNode.Children
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
