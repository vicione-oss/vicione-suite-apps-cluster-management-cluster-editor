using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.Blazor.Components.ContextMenu.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.ContextMenu;

public sealed class AddDataPortContextMenuContext : IContextMenuContext
{
    public required IEnumerable<DataPortContextMenuItem> AddDataPortContextMenuItems { get; set; }
    public ContextMenuItemFilter? ItemFilter { get; init; }
    public required MouseEventArgs MouseEventArgs { get; init; }
}
