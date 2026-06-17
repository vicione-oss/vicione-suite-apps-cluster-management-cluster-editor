using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Models;

public sealed class ContainerEditorChildContainerBackup
{
    public string? BackColor { get; init; }
    public required ChildContainer Container { get; init; }
    public string? Description { get; init; }
    public string? ForeColor { get; init; }
    public string Name { get; init; } = string.Empty;
}
