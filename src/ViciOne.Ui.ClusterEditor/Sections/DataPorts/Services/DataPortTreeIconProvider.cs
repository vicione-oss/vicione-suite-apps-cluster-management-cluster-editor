using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Models.Data;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ColorableIcons;
using ViciOne.Ui.TreeEditor.Builder.Interface.Icons;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

internal static class DataPortTreeIconProvider
{
    internal static IIcon? GetDataPointIcon(DataPortChildNodeModel childNode, int size, IClusterCache clusterCache)
    {
        if (!childNode.IsDataPoint)
            return null;

        var dataTypeValue = childNode.GetSystemProperty<string>(nameof(DataPortTreeNode.ValueType));
        var shouldSend = childNode.GetSystemProperty<DataPortTransferMode>()!.TypedValue is not DataPortTransferMode.None;
        if (dataTypeValue is not null)
        {
            var color = ConnectorColor.Get(typeof(object));
            if (!string.IsNullOrEmpty(dataTypeValue.TypedValue))
            {
                var runtimeType = childNode.RootNode.Builder.DataTypes[dataTypeValue.TypedValue].RuntimeType;
                if (runtimeType is not null)
                    color = ConnectorColor.Get(runtimeType);
            }
            color = shouldSend ? color : DataPortColors.Disabled;

            var dataPortDirection = childNode.GetRootSuccessor().GetSystemProperty<DataPortDirection>()?.TypedValue ?? DataPortDirection.InOut;

            var isConnectedToOuputConnectors = clusterCache.DataPortTreeNodeIds[childNode.Id.Value].IncomingLinks.Count != 0;
            var isConnectedToInputConnectors = clusterCache.DataPortTreeNodeIds[childNode.Id.Value].OutgoingLinks.Count != 0;

            var icon = ColoredIconFactory.GetDataPortIcon(color, dataPortDirection, isConnectedToOuputConnectors, isConnectedToInputConnectors, size);
            if (!string.IsNullOrEmpty(icon))
                return new SvgIcon(icon) { UseIncludedColors = true };
        }

        return null;
    }

    internal static string? GetSvgIcon(TreeBuilder.TreeBuilder treeBuilder, string iconName)
        => treeBuilder.GetSvgIcon(iconName);
}
