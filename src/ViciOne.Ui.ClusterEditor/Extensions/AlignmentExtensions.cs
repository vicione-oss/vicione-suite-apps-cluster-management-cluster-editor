using System.Linq;
using Blazor.Diagrams.Core.Models;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Services;

internal static class AlignmentExtensions
{
    public static void ApplyToSelection(this Alignment alignment, SelectionManager selectionManager)
    {
        var selectedNodes = Enumerable.Empty<NodeModel>()
            .Concat(selectionManager.SelectedBlockNodes)
            .Concat(selectionManager.SelectedLabels);

        if (selectedNodes.Count() >= 2)
            selectedNodes.AlignNodes(alignment);
    }
}
