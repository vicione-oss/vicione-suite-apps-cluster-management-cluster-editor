using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.ContextMenu.Components;
using ViciOne.Ui.ClusterEditor.Models.Data;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ColorableIcons;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.ContextMenu;

public sealed partial class DataPortAddChildNodeContextMenu : SpecializedContextMenuBase<DataPortAddChildNodeContextMenuContext>
{
    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;

    [Parameter]
    public EventCallback<DataPortAddChildNodeContextMenuItemClickEventArgs> OnContextMenuItemClick { get; set; }

    private async Task ContextMenuItemClickAsync(DataPortChildNodeModel possibleChild)
    {
        if (OnContextMenuItemClick.HasDelegate && Context is not null)
        {
            await OnContextMenuItemClick.InvokeAsync(new DataPortAddChildNodeContextMenuItemClickEventArgs
            {
                ParentNode = Context.ParentNode,
                PossibleChild = possibleChild
            });
        }
    }

    public static string GetIconData(DataPortChildNodeModel possibleChild)
    {
        var color = ConnectorColor.Get(typeof(object));
        var dataTypeValue = possibleChild.GetSystemProperty<string>(nameof(DataPortTreeNode.ValueType));

        if (dataTypeValue is not null && !string.IsNullOrEmpty(dataTypeValue.TypedValue))
        {
            var runtimeType = possibleChild.RootNode.Builder.DataTypes[dataTypeValue.TypedValue].RuntimeType;
            color = ConnectorColor.Get(runtimeType ?? typeof(object));
        }

        var dataPortDirection = possibleChild.GetRootSuccessor().GetSystemProperty<DataPortDirection>()?.TypedValue;

        var icon = possibleChild.Icon != "datapoint"
            ? possibleChild.RootNode.Builder.GetSvgIcon(possibleChild.Icon ?? string.Empty)
            : ColoredIconFactory.GetDataPortIcon(color, dataPortDirection ?? DataPortDirection.In, true, true);

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
