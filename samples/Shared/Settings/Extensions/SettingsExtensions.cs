using ViciOne.Ui.ClusterEditor.Models;

namespace Shared.Settings.Extensions;

internal static class SettingsExtensions
{
    internal static bool AreChanged(this Models.Settings settings, Models.Settings other)
        => settings.CurrentCultureName != other.CurrentCultureName ||
           settings.GridMode != other.GridMode ||
           settings.MinimapNodeColoring != other.MinimapNodeColoring ||
           settings.NodeAlignmentBorder != other.NodeAlignmentBorder ||
           settings.PanBehavior != other.PanBehavior ||
           settings.ShowDefaultContextMenu != other.ShowDefaultContextMenu ||
           settings.SimplifiedView != other.SimplifiedView;

    internal static Models.Settings Clone(this Models.Settings settings)
        => new()
        {
            CurrentCultureName = settings.CurrentCultureName,
            GridMode = settings.GridMode,
            MinimapNodeColoring = settings.MinimapNodeColoring,
            NodeAlignmentBorder = settings.NodeAlignmentBorder,
            PanBehavior = settings.PanBehavior,
            ShowDefaultContextMenu = settings.ShowDefaultContextMenu,
            SimplifiedView = settings.SimplifiedView,
        };

    internal static GridMode GetGridMode(this Models.Settings settings)
        => (GridMode)settings.GridMode;
}
