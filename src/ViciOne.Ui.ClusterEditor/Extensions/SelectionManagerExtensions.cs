using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Ui.Blazor.Components.ContextMenu.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;

internal static class SelectionManagerExtensions
{
    public static ContextMenuItemFilter GetContextMenuItemFilterForSelection(this SelectionManager selectionManager)
        => new() { ApplicableTo = selectionManager.SelectedModels.Any() ? selectionManager.GetDiagramModelTypesFromSelection() : null };

    public static IEnumerable<Type> GetDiagramModelTypesFromSelection(this SelectionManager selectionManager)
    {
        var result = new List<Type>();

        if (selectionManager.SelectedContainers.Any())
            result.Add(typeof(ChildContainerNode));

        if (selectionManager.SelectedFBs.Any())
            result.Add(typeof(FunctionBlockNode));

        if (selectionManager.SelectedConnectors.Any())
            result.Add(typeof(BlockNodeConnector));

        if (selectionManager.SelectedLabels.Any())
            result.Add(typeof(LabelNode));

        return result;
    }
}
