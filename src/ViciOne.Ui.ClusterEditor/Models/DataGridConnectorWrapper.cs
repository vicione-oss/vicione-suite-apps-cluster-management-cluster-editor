using System;
using System.Collections.Generic;
using System.Globalization;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Core.Contracts;
using ViciOne.Core.Dataflow.DataModel;

using Connector = ViciOne.Cluster.Model.Connector;

namespace ViciOne.Ui.ClusterEditor.Models;

public sealed class DataGridConnectorWrapper(IConnector connector,
    ConnectorDesign connectorDesign,
    FunctionBlockDesign functionBlockDesign,
    ConnectorMarkerType destinationMarker = ConnectorMarkerType.None,
    string? dataflowName = null) : IDragable
{
    private string? _dataflowName = dataflowName;
    private string? _path;

    public IConnector Connector { get; } = connector;
    public string ConnectorName => Connector.Name;
    public Type ConnectorType => connectorDesign.ConnectorType;
    public string ConnectorTypeName => DataTypeCompatibilityValidator.DetermineValueType(ConnectorType).Name;
    public string Description => Connector.Description ?? string.Empty;
    public string DesignName => functionBlockDesign.Name;
    public ConnectorMarkerType DestinationMarker { get; } = destinationMarker;
    public Guid FunctionBlockId => Connector.FunctionBlock.Id;
    public string FunctionBlockName => Connector.FunctionBlock.Name;
    public bool IsInput => Connector is IConnectorInput;

    public int Links
    {
        get
        {
            var count = 0;
            foreach (var link in Connector.Links)
            {
                if (!link.Visible)
                    count++;
            }
            return count;
        }
    }

    /// <summary>
    /// The link count as it is displayed and searched, so the cell, the column filter and the search all match
    /// the same text.
    /// </summary>
    public string LinksText => Links.ToString(CultureInfo.CurrentCulture);

    public string ParentName => ((INamedContainerChild)Connector.Parent).Name;

    /// <summary>
    /// The container path as it is displayed, searched and filtered. Resolved on first read and kept.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The dataflow name is resolved by the caller and handed in as a string, never read from the cluster here.
    /// That is deliberate: this property is read twice per row on every render of the Path column and once more
    /// per row by its filter, while a wrapper outlives the cluster it was built from — the sections keep their
    /// instances so the table's selection survives. Looking the dataflow up lazily meant that read threw from
    /// inside the render tree and took the Blazor circuit with it.
    /// </para>
    /// <para>
    /// Cached rather than computed in the constructor so building a wrapper stays free of the upstream-container
    /// walk: callers build them in loops, and a wrapper whose path is never displayed should cost nothing.
    /// </para>
    /// </remarks>
    public string Path => _path ??= BuildPath(Connector, _dataflowName);

    private static string BuildPath(IConnector connector, string? dataflowName)
    {
        var containers = ContainerChildExtensions.GetAllUpstreamContainers(connector.FunctionBlock);
        var parts = new List<string>();
        foreach (var c in containers)
            parts.Add(c.Name);
        parts.Reverse();

        // The topmost container is the dataflow root, whose internal name ('Root') is meaningless to the user.
        if (parts.Count > 0 && !string.IsNullOrEmpty(dataflowName))
            parts[0] = dataflowName;

        return string.Join('.', parts);
    }

    /// <summary>
    /// Points <see cref="Path"/> at a new dataflow name and drops the cached value, keeping this instance so the
    /// table's selection survives. The path is rebuilt on the next read.
    /// </summary>
    /// <param name="dataflowName">
    /// The name of the dataflow the connector's function block belongs to, or <see langword="null"/> to fall back
    /// to the root container's own name.
    /// </param>
    public void RefreshPath(string? dataflowName)
    {
        _dataflowName = dataflowName;
        _path = null;
    }
}
