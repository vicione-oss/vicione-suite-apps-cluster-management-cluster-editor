using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Models;

public sealed class DataGridDataPortWrapper(DataPortTreeNode dataPortTreeNode, string path)
{
    public DataPortTreeNode DataPortTreeNode => dataPortTreeNode;
    public string Description => dataPortTreeNode.Description ?? string.Empty;
    public string Name => dataPortTreeNode.Name;
    public string Path => path;
}
