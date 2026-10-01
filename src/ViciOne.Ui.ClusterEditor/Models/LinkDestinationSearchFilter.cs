using System;
using ViciOne.Ui.Blazor.Components.Tables.Shared.Models;
using ViciOne.Ui.Blazor.Components.Tables.SimpleTable.Models;

namespace ViciOne.Ui.ClusterEditor.Models;

/// <summary>
/// Matches a link destination row against the dialog's search text — connectors on design, path, function
/// block, connector name or description; data ports on name, path or description.
/// </summary>
internal sealed record LinkDestinationSearchFilter(string SearchText) : IGlobalFilter, ISimpleTableFilter<object>
{
    /// <inheritdoc/>
    public bool Matches(object item)
    {
        if (item is DataGridConnectorWrapper connector)
        {
            return connector.DesignName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || connector.Path.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || connector.FunctionBlockName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || connector.ConnectorName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || connector.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }

        if (item is DataGridDataPortWrapper dataPort)
        {
            return dataPort.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || dataPort.Path.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || dataPort.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
