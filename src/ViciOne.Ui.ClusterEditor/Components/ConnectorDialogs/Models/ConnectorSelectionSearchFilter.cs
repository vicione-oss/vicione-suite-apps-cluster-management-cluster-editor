using System;
using ViciOne.Ui.Blazor.Components.Tables.Shared.Models;
using ViciOne.Ui.Blazor.Components.Tables.SimpleTable.Models;
using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Components.ConnectorDialogs.Models;

/// <summary>
/// Matches a connector row against the dialog's search text — on element, connector name, connector type or
/// description.
/// </summary>
internal sealed record ConnectorSelectionSearchFilter(string SearchText)
    : IGlobalFilter, ISimpleTableFilter<DataGridConnectorWrapper>
{
    /// <inheritdoc/>
    public bool Matches(DataGridConnectorWrapper item)
        => item.ParentName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.ConnectorName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.ConnectorTypeName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
}
