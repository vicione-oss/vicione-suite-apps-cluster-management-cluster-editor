using System.Collections.Generic;
using System.Drawing;

namespace ViciOne.Ui.ClusterEditor.Models;

public sealed class TooltipInfo
{
    public List<List<string>?> Content { get; } = [];
    public string? Header { get; set; }
    public required Rectangle ParentBounds { get; set; }
    public int PosX { get; set; }
    public int PosY { get; set; }
    public TooltipInfoType Type { get; set; }
}
