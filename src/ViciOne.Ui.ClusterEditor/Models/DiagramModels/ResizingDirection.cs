using System;

namespace ViciOne.Ui.ClusterEditor.Models.DiagramModels;

[Flags]
public enum ResizingDirection
{
    Left = 1,
    Top = 2,
    Right = 4,
    Bottom = 8
}
