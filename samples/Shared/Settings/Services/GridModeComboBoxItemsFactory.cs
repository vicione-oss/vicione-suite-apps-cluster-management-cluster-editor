using Shared.Settings.Extensions;
using ViciOne.Ui.Blazor.Components.ComboBox;
using ViciOne.Ui.ClusterEditor.Models;

namespace Shared.Settings.Services;

internal static class GridModeComboBoxItemsFactory
{
    internal static List<ComboBoxItem<int, string>> GridModeComboBoxItems => [..
        Enum.GetValues<GridMode>().Select(gm => new ComboBoxItem<int, string>
        {
            Text = gm.ToLocalizedString(),
            Value = (int)gm,
        })];
}
