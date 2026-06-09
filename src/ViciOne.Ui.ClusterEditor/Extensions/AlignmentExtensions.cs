using System.Linq;
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

        var selectedNodes = Enumerable.Empty<NodeModel>()
            .Concat(selectionManager.SelectedBlockNodes)
            .Concat(selectionManager.SelectedLabels);

        selectedNodes.AlignNodes(alignment);
    }
}
