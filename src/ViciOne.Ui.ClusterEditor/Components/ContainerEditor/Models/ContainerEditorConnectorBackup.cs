using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Models;

public class ContainerEditorConnectorBackup
{
    public required ContainerConnector Connector { get; init; }
    public string? Description { get; init; }
    public static ContainerEditorConnectorBackup Empty
        => new()
        {
            Connector = new ContainerConnectorInput(),
            Index = 0,
            Name = string.Empty,
        };
    public required int Index { get; init; }
    public required string Name { get; init; }
    public string ShortName { get; init; } = string.Empty;
}
