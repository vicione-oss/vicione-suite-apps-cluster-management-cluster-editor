using ViciOne.Cluster.Builder;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

internal sealed class DataPortChildNodeEditContext
{
    public required ClusterBuilder ClusterBuilder { get; init; }
    public required DataPortChildNodeModel Node { get; init; }
}
