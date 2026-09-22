using System.Collections.Generic;
using System.Collections.ObjectModel;
using ViciOne.Cluster.Builder.Abstractions;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

internal sealed class DataPortChildNodeEditContext
{
    public required IClusterBuilder ClusterBuilder { get; init; }
    public required DataPortChildNodeModel Node { get; init; }

    /// <summary>
    /// Values by property name that were changed but not saved yet, shown in place of the values of <see cref="Node"/>.
    /// </summary>
    public IReadOnlyDictionary<string, object?> PendingValues { get; init; } = ReadOnlyDictionary<string, object?>.Empty;
}
