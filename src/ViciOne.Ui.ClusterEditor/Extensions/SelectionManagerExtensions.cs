using System;
using System.Collections.Generic;
using ViciOne.Ui.Blazor.Components.ContextMenu.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;

internal static class SelectionManagerExtensions
{
    public static ContextMenuItemFilter GetContextMenuItemFilterForSelection(this SelectionManager selectionManager)
        => new() { ApplicableTo = selectionManager.HasSelection ? selectionManager.GetDiagramModelTypesFromSelection() : null };

    public static IEnumerable<Type> GetDiagramModelTypesFromSelection(this SelectionManager selectionManager)
    {
        var result = new List<Type>();

        if (selectionManager.SelectedContainers.Count > 0)
            result.Add(typeof(ChildContainerNode));

        if (selectionManager.SelectedFBs.Count > 0)
            result.Add(typeof(FunctionBlockNode));

        if (selectionManager.SelectedConnectors.Count > 0)
            result.Add(typeof(BlockNodeConnector));

        if (selectionManager.SelectedLabels.Count > 0)
            result.Add(typeof(LabelNode));

        return result;
    }
}
