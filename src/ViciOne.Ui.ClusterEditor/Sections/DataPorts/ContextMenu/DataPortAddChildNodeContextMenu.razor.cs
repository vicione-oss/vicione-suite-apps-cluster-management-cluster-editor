using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ViciOne.Cluster.Model;
using ViciOne.TreeBuilder.NodeTypes;
using ViciOne.Ui.Blazor.Components.ContextMenu.Components;
using ViciOne.Ui.ClusterEditor.Models.Data;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ColorableIcons;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.ContextMenu;

public sealed partial class DataPortAddChildNodeContextMenu : SpecializedContextMenuBase<DataPortAddChildNodeContextMenuContext>
{
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

    private static string GetIconData(DataPortChildNodeContextMenuDescriptor descriptor)
    {
        var rootNode = descriptor.ParentNode.GetRootNode();

        if (!rootNode.Builder.NodeTypes.TryGetValue(descriptor.NodeReference.Id, out var nodeType))
            return string.Empty;

        var dataPortDirection = (descriptor.ParentNode as DataPortChildNodeModel)?.GetRootSuccessor().GetSystemProperty<DataPortDirection>()?.TypedValue ?? DataPortDirection.In;

        var runtimeType = typeof(object);
        if (nodeType is DataPortTreeNodeType dataPortTreeNodeType
            && dataPortTreeNodeType.DataTypes.Length > 0
            && rootNode.Builder.DataTypes.TryGetValue(dataPortTreeNodeType.DataTypes[0], out var dataType)
            && dataType.RuntimeType is not null)
        {
            runtimeType = dataType.RuntimeType;
        }

        var color = ConnectorColor.Get(runtimeType);
        var icon = descriptor.IconName != "datapoint"
            ? rootNode.Builder.GetSvgIcon(descriptor.IconName)
            : ColoredIconFactory.GetDataPortIcon(color, dataPortDirection, true, true);

        icon = icon?.Replace("viewBox=\"0 0 32 32\"", "viewBox=\"4 4 28 28\" width=\"16\" height=\"16\"",
            StringComparison.InvariantCulture);
        icon = icon?.Replace("currentColor", DataPortColorConstants.ColorEditorFont,
            StringComparison.InvariantCulture);

        var iconBase64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(icon ?? string.Empty));

        return $"data:image/svg+xml;base64,{iconBase64Encoded}";
    }

    private void OnContextMenuVisibilityChanged(bool isVisible)
    {
        if (!isVisible)
            DiagramEventService.RequestDiagramFocus();
    }
}
