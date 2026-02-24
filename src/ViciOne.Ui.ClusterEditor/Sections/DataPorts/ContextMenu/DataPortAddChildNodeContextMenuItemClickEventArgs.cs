using System;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.ContextMenu;

public sealed class DataPortAddChildNodeContextMenuItemClickEventArgs : EventArgs
{
    public required DataPortNodeModel ParentNode { get; init; }
    public required DataPortChildNodeModel PossibleChild { get; init; }
}
