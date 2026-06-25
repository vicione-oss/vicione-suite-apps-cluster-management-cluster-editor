using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;

namespace ViciOne.Ui.ClusterEditor.Sections.Dataflow.Models;

internal sealed class DataflowStructureTreeNode : IClusterEditorTreeNode
{
    public bool Active { get; set; }
    public required Cluster.Model.Dataflow Dataflow { get; set; }
    public bool Deleting { get; set; }
    public bool Editing { get; set; }
    public bool Expanded { get; set; }
    public bool Highlighted { get; set; }
    public INodeIdentifier Id { get; set; } = GuidNodeIdentifier.New();
    public string Name { get; set; } = string.Empty;
    public bool Selected { get; set; }
}
