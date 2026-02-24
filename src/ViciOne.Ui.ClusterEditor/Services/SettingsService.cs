using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Services;

internal class SettingsService(DiagramService diagramService) : ISettingsService
{
    private readonly DiagramService _diagramService = diagramService;

    public GridMode GetGridMode()
        => _diagramService.DiagramState.GridMode;

    public bool GetMinimapNodeColoring()
        => _diagramService.DiagramState.UseNodeColoringOnMinimap;

    public bool GetNodeAlignmentBorder()
        => _diagramService.DiagramState.IsNodeAlignmentBorderEnabled;

    public bool GetPanBehavior()
        => _diagramService.DiagramState.UsesGimpPanBehavior;

    public bool GetSimplifiedView()
        => _diagramService.DiagramState.SimplifiedView;

    public void SetGridMode(GridMode gridMode)
        => _diagramService.RequestGridModeChange(gridMode);

    public void SetMinimapNodeColoring(bool minimapNodeColoring)
        => _diagramService.SetMinimapNodeColoring(minimapNodeColoring);

    public void SetNodeAlignmentBorder(bool nodeAlignmentBorder)
        => _diagramService.SetNodeAlignmentBorderActive(nodeAlignmentBorder);

    public void SetPanBehavior(bool panBehavior)
        => _diagramService.RequestPanBehaviorChange(panBehavior);

    public void SetSimplifiedView(bool simplifiedView)
        => _diagramService.RequestSimplifiedViewChange(simplifiedView);
}
