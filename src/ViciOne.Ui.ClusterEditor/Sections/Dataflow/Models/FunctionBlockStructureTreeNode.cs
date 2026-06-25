using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;

namespace ViciOne.Ui.ClusterEditor.Sections.Dataflow.Models;

internal sealed class FunctionBlockStructureTreeNode : IClusterEditorTreeNode
{
    public bool Expanded { get; set; }
    public required FunctionBlock FunctionBlock { get; set; }
    public bool Highlighted { get; set; }
    public INodeIdentifier Id { get; set; } = GuidNodeIdentifier.New();
    public string Name { get; set; } = string.Empty;
    public bool Selected { get; set; }
}
