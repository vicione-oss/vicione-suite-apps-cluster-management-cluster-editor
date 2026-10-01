using System;
using System.Linq;
using ViciOne.Ui.Blazor.Components.Tables.Shared.Models;
using ViciOne.Ui.Blazor.Components.Tables.SimpleTable.Models;
using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor.Models;

/// <summary>
/// Matches a settings row against the editor's search text — on the setting name shared by the row, or on
/// the value held by any of its function blocks.
/// </summary>
internal sealed record FbSettingSearchFilter(string SearchText)
    : IGlobalFilter, ISimpleTableFilter<IGrouping<string, FbSetting>>
{
    /// <inheritdoc/>
    public bool Matches(IGrouping<string, FbSetting> item)
        => item.Key.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.Any(setting =>
                setting.Value?.ToString()?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);
}
