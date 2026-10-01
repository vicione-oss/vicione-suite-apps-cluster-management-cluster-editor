using ViciOne.Ui.Blazor.Components.Tables.Shared.Models;
using ViciOne.Ui.Blazor.Components.Tables.SimpleTable.Models;
using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Models;

/// <summary>
/// Keeps only the rows whose connector points in the requested direction.
/// </summary>
/// <remarks>
/// Global filters are keyed by their type, so switching the direction replaces this filter alone and leaves
/// the search and the column filters standing.
/// </remarks>
internal sealed record PublishedConnectorDirectionFilter(ConnectorDirection Direction)
    : IGlobalFilter, ISimpleTableFilter<DataGridConnectorWrapper>
{
    /// <inheritdoc/>
    public bool Matches(DataGridConnectorWrapper item)
        => item.IsInput == (Direction == ConnectorDirection.Input);
}
