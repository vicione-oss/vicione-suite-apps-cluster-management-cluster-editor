using System.Globalization;

namespace Shared.Settings.Models;

internal sealed class Settings
{
    public string CurrentCultureName { get; set; } = CultureInfo.CurrentCulture.Name;
    public int GridMode { get; set; } = 1;
    public bool MinimapNodeColoring { get; set; }
    public bool NodeAlignmentBorder { get; set; } = true;
    public bool PanBehavior { get; set; } = true;
    public bool ShowDefaultContextMenu { get; set; } = true;
    public bool SimplifiedView { get; set; }
}
