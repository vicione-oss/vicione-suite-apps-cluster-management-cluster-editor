using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;

namespace ViciOne.Ui.ClusterEditor.Models.ComponentStates;

public sealed class DiagramState
{
    public GridMode GridMode { get; set; } = GridMode.Line;
    public bool IsContextMenuVisible { get; set; }
    public bool IsInitialized { get; set; }
    public bool IsMinimapVisible { get; set; } = true;
    public bool IsNodeAlignmentBorderEnabled { get; set; } = true;
    public bool IsNodeAlignmentBorderVisible { get; set; }
    public bool LabelsLocked { get; set; }
    public LabelNode? NewlyCreatedLabel { get; set; }
    public bool SimplifiedView { get; set; }
    public bool SuppressEvents { get; set; }
    public bool UseNodeColoringOnMinimap { get; set; }
    public bool? UsesGimpPanBehavior { get; set; }
    public double Zoom { get; set; } = DiagramSettings.DefaultZoom;
}
