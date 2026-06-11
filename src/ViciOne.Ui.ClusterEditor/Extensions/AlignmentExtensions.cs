using System.Collections.Generic;
using Blazor.Diagrams.Core.Models;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Services;

internal static class AlignmentExtensions
{
    public static void ApplyToSelection(this Alignment alignment, SelectionManager selectionManager)
    {
        if (selectionManager.SelectedBlockNodes.Count + selectionManager.SelectedLabels.Count < 2)
            return;

        var selectedNodes = new List<NodeModel>(selectionManager.SelectedBlockNodes.Count + selectionManager.SelectedLabels.Count);
        foreach (var node in selectionManager.SelectedBlockNodes)
            selectedNodes.Add(node);
        foreach (var label in selectionManager.SelectedLabels)
            selectedNodes.Add(label);

        selectedNodes.AlignNodes(alignment);
    }
}
