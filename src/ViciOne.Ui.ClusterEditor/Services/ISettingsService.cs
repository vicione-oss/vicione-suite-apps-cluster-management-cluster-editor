using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Services;

public interface ISettingsService
{
    GridMode GetGridMode();
    bool GetMinimapNodeColoring();
    bool GetNodeAlignmentBorder();
    bool GetPanBehavior();
    bool GetSimplifiedView();
    void SetGridMode(GridMode gridMode);
    void SetMinimapNodeColoring(bool minimapNodeColoring);
    void SetNodeAlignmentBorder(bool nodeAlignmentBorder);
    void SetPanBehavior(bool panBehavior);
    void SetSimplifiedView(bool simplifiedView);
}
