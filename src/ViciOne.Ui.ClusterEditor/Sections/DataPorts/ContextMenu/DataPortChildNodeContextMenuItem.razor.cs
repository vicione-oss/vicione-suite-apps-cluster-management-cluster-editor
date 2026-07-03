using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.ContextMenu;

public sealed partial class DataPortChildNodeContextMenuItem
{
    [Parameter] public required DataPortChildNodeContextMenuDescriptor Descriptor { get; set; }
    [Parameter] public required Func<DataPortChildNodeContextMenuDescriptor, string> GetIconDataFunc { get; set; }
    [Parameter] public required EventCallback<DataPortChildNodeContextMenuDescriptor> OnClick { get; set; }

    private Task HandleClickAsync() => OnClick.InvokeAsync(Descriptor);
}
