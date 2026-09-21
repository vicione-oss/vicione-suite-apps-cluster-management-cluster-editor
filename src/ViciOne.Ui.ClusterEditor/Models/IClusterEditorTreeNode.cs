using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace ViciOne.Ui.ClusterEditor.Models;

internal interface IClusterEditorTreeNode : ITreeNode
{
    bool Expanded { get; set; }
    string Name { get; set; }
    bool Selected { get; set; }
}
