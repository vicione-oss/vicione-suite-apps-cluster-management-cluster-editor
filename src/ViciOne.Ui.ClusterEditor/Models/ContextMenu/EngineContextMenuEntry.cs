namespace ViciOne.Ui.ClusterEditor.Models.ContextMenu;

public class EngineContextMenuEntry
{
    public bool BeginGroup { get; set; }
    public required Cluster.Model.Engine Engine { get; set; }
    public string? IconCssClass { get; set; }
    public required string Text { get; set; }
}
