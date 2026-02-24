using ViciOne.Ui.ClusterEditor.Components.ContextMenu;

namespace ViciOne.Ui.ClusterEditor.Helpers;

internal static class ContextMenuHelper
{
    public static string? GetIconUrl(int acutalCount, int maxCount)
    {
        if (acutalCount == maxCount)
            return ContextMenuIcon.Apply;
        else if (acutalCount > 0)
            return ContextMenuIcon.Substract;
        else
            return null;
    }
}
