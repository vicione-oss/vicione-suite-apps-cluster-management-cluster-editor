using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.Blazor.Components.ContextMenu.Components;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.ContextMenu;

public sealed partial class DataPortAddChildNodeContextMenu : SpecializedContextMenuBase<DataPortAddChildNodeContextMenuContext>
{
    // Keyed by reference: the descriptors are rebuilt whenever a node's PossibleChildren is
    // reassigned, so an instance stands for exactly one entry of one menu.
    private readonly Dictionary<DataPortChildNodeContextMenuDescriptor, string> _iconData = [];

    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;

    [Parameter]
    public EventCallback<DataPortAddChildNodeContextMenuItemClickEventArgs> OnContextMenuItemClick { get; set; }

    private async Task ContextMenuItemClickAsync(DataPortChildNodeContextMenuDescriptor possibleChild)
    {
        if (Context is null)
            return;

        var dataPortChild = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(possibleChild, Context.ParentNode);

        if (OnContextMenuItemClick.HasDelegate)
        {
            await OnContextMenuItemClick.InvokeAsync(new DataPortAddChildNodeContextMenuItemClickEventArgs
            {
                ParentNode = Context.ParentNode,
                PossibleChild = dataPortChild
            });
        }
    }

    /// <summary>
    /// Cached for the lifetime of one open menu: the item asks for its icon on every render, and
    /// building it means building the node the entry would create.
    /// </summary>
    private string GetIconData(DataPortChildNodeContextMenuDescriptor descriptor)
    {
        if (_iconData.TryGetValue(descriptor, out var iconData))
            return iconData;

        return _iconData[descriptor] = DataPortMenuIconProvider.GetIconData(descriptor);
    }

    private void OnContextMenuVisibilityChanged(bool isVisible)
    {
        if (isVisible)
            return;

        _iconData.Clear();
        DiagramEventService.RequestDiagramFocus();
    }
}
