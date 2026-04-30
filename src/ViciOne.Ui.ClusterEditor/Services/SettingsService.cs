using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Services;

internal class SettingsService(DiagramService diagramService) : ISettingsService
{
    public GridMode GetGridMode()
        => diagramService.DiagramState.GridMode;

    public bool GetMinimapNodeColoring()
        => diagramService.DiagramState.UseNodeColoringOnMinimap;

    public bool GetNodeAlignmentBorder()
        => diagramService.DiagramState.IsNodeAlignmentBorderEnabled;

    public bool GetPanBehavior()
        => diagramService.DiagramState.UsesGimpPanBehavior.GetValueOrDefault();

    public bool GetSimplifiedView()
        => diagramService.DiagramState.SimplifiedView;

    public void SetGridMode(GridMode gridMode)
        => diagramService.RequestGridModeChange(gridMode);

    public void SetMinimapNodeColoring(bool minimapNodeColoring)
        => diagramService.SetMinimapNodeColoring(minimapNodeColoring);

    public void SetNodeAlignmentBorder(bool nodeAlignmentBorder)
        => diagramService.SetNodeAlignmentBorderActive(nodeAlignmentBorder);

    public void SetPanBehavior(bool panBehavior)
        => diagramService.RequestPanBehaviorChange(panBehavior);

    public void SetSimplifiedView(bool simplifiedView)
        => diagramService.RequestSimplifiedViewChange(simplifiedView);
}
