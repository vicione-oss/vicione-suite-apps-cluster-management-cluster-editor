using ViciOne.Ui.ClusterEditor.Models;

namespace Shared.Settings.Extensions;

internal static class GridModeExtensions
{
    internal static string ToLocalizedString(this GridMode gridMode)
    {
        var gridModeNameKey = $"GridMode{gridMode}";

        return Components.Localization.SettingsDialog.ResourceManager.GetString(gridModeNameKey,
           Components.Localization.SettingsDialog.Culture) ?? gridMode.ToString();
    }
}
