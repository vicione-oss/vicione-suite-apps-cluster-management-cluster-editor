using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.Ui.Blazor.Components.Draggable.Components;
using ViciOne.Ui.Blazor.Components.Draggable.Services;
using ViciOne.Ui.Blazor.Components.Tables.Shared.Models;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;

namespace ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;

/// <summary>
/// Admits a connector as a drop target for a drag of published-connector rows.
/// </summary>
/// <remarks>
/// Asked once per connector at drag start. The table freezes the payload before any connector is asked, so the
/// payload's identity is a sound cache key: the valid targets are resolved once per drag.
/// </remarks>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class PublishedConnectorDropPolicy(PublishedConnectorsService publishedConnectorsService)
    : IDropPolicy<BlockNodeConnector>
{
    private IReadOnlyList<DataGridConnectorWrapper>? _resolvedForPayload;
    private IReadOnlySet<BlockNodeConnector> _validTargetConnectors = new HashSet<BlockNodeConnector>();

    /// <inheritdoc/>
    public bool Accepts(IDraggable draggable, BlockNodeConnector target)
    {
        if (draggable is not IDraggableRowSet<DataGridConnectorWrapper> rowSet)
            return false;

        var payload = rowSet.Items;

        if (_resolvedForPayload != payload)
        {
            _validTargetConnectors = publishedConnectorsService.GetValidTargetConnectors(payload);
            _resolvedForPayload = payload;
        }

        return _validTargetConnectors.Contains(target);
    }
}
