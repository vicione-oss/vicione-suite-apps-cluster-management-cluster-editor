using ViciOne.Cluster.Builder.Abstractions;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

internal sealed class DataPortChildNodeEditContext
{
    public required IClusterBuilder ClusterBuilder { get; init; }
    public required DataPortChildNodeModel Node { get; init; }
}
