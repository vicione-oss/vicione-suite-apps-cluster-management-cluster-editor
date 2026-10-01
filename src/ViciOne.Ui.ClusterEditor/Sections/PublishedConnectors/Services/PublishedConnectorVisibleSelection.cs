using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ViciOne.Ui.Blazor.Components.Tables.Shared.Services;
using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;

/// <summary>
/// Mirrors the rows the table last reported as visibly selected, and scopes a drag to them: a selected row
/// the active filter hides must not ride along with nothing on screen to say so.
/// </summary>
/// <remarks>
/// The mirror lives here rather than in the section component because the payload rule is asked for as a
/// service. The table resolves row identity with its own selection comparer, so the reported instances are
/// the ones to compare against — not equivalents built elsewhere.
/// </remarks>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class PublishedConnectorVisibleSelection : ITableDragPayloadProvider<DataGridConnectorWrapper>
{
    /// <summary>The rows the table last reported as visibly selected.</summary>
    public IReadOnlyList<DataGridConnectorWrapper> Rows { get; private set; } = [];

    /// <inheritdoc/>
    // Needs no guard for a drag on an unselected row: that row is single-selected first, and the raise
    // carrying it into the mirror is awaited before this runs.
    public IReadOnlyList<DataGridConnectorWrapper> GetPayload(
        DataGridConnectorWrapper draggedRow,
        IReadOnlyList<DataGridConnectorWrapper> resolvedPayload)
        => [.. resolvedPayload.Where(Rows.Contains)];

    public void Store(IReadOnlyList<DataGridConnectorWrapper> visibleSelection)
        => Rows = visibleSelection;
}
