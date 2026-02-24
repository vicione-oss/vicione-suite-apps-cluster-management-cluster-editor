using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.Blazor.Components.ContextMenu.Models;

namespace ViciOne.Ui.ClusterEditor.Models.ContextMenu.Specialized;

public class BlockNodeConnectorContextMenuContext : IContextMenuContext
{
    public ContextMenuItemFilter? ItemFilter { get; init; }
    public required MouseEventArgs MouseEventArgs { get; init; }
}
