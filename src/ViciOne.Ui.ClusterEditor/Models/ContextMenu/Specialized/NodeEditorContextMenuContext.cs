using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.Blazor.Components.ContextMenu.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;

namespace ViciOne.Ui.ClusterEditor.Models.ContextMenu.Specialized;

public class NodeEditorContextMenuContext : IContextMenuContext
{
    public Block? Block { get; init; }
    public ContextMenuItemFilter? ItemFilter { get; init; }
    public required MouseEventArgs MouseEventArgs { get; init; }
    public object? ObjectOpenedOn { get; init; }
}
