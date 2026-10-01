using System;
using ViciOne.Ui.Blazor.Components.Tables.Shared.Models;
using ViciOne.Ui.Blazor.Components.Tables.SimpleTable.Models;
using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Models;

/// <summary>
/// Matches a published-connector row against the section's search text — on path, design, function block,
/// connector name, link count or description.
/// </summary>
internal sealed record PublishedConnectorSearchFilter(string SearchText)
    : IGlobalFilter, ISimpleTableFilter<DataGridConnectorWrapper>
{
    /// <inheritdoc/>
    public bool Matches(DataGridConnectorWrapper item)
        => item.Path.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.DesignName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.FunctionBlockName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.ConnectorName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.LinksText.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
}
