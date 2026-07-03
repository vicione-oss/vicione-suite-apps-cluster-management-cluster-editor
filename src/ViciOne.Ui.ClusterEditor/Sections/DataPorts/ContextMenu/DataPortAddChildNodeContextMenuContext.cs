using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.Blazor.Components.ContextMenu.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.ContextMenu;

public sealed class DataPortAddChildNodeContextMenuContext : IContextMenuContext
{
    public ContextMenuItemFilter? ItemFilter { get; init; }
    public required MouseEventArgs MouseEventArgs { get; init; }
    public required DataPortNodeModel ParentNode { get; init; }
    public required IReadOnlyList<DataPortChildNodeContextMenuDescriptor> PossibleChildren { get; init; }
}
