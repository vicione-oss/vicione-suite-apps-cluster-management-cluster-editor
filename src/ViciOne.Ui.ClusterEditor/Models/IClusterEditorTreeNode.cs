using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace ViciOne.Ui.ClusterEditor.Models;

internal interface IClusterEditorTreeNode : ITreeNode
{
    string Name { get; set; }
}
