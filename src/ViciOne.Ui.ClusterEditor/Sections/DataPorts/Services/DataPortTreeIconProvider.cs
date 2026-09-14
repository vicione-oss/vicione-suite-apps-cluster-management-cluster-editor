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

        var clusterNode = clusterCache.DataPortTreeNodeIds[childNode.Id.Value];
        var icon = GetDataPointIconMarkup(
            childNode,
            size,
            hasInboundLink: clusterNode.OutgoingLinks.Count != 0,
            hasOutboundLink: clusterNode.IncomingLinks.Count != 0);

        return string.IsNullOrEmpty(icon) ? null : new SvgIcon(icon) { UseIncludedColors = true };
    }

    /// <summary>
    /// The datapoint icon of <paramref name="childNode"/>, or <see langword="null"/> when the node
    /// carries no value to draw one from. Taking the link state as an argument lets the insert menu
    /// ask for the icon of a node that does not exist yet, and so cannot have links.
    /// </summary>
    internal static string? GetDataPointIconMarkup(DataPortChildNodeModel childNode, int? size, bool hasInboundLink, bool hasOutboundLink)
    {
        if (!childNode.IsDataPoint)
            return null;

        var dataTypeValue = childNode.GetSystemProperty<string>(nameof(DataPortTreeNode.ValueType));
        if (dataTypeValue is null)
            return null;

        var color = ConnectorColor.Get(typeof(object));
        if (!string.IsNullOrEmpty(dataTypeValue.TypedValue))
        {
            var runtimeType = childNode.RootNode.Builder.DataTypes[dataTypeValue.TypedValue].RuntimeType;
            if (runtimeType is not null)
                color = ConnectorColor.Get(runtimeType);
        }

        var rootDirection = childNode.GetRootSuccessor().GetSystemProperty<DataPortDirection>()?.TypedValue ?? DataPortDirection.InOut;
        var dataPortDirection = childNode.GetEffectiveIconDirection(rootDirection);

        // A node left with no effective direction transfers nowhere - an Outbound-only
        // envelope child on an inbound-only DataPort, say. It is as switched off as a node
        // whose transfer mode is None, and must not read as an ordinary datapoint.
        color = ShouldSend(childNode) && dataPortDirection is not null ? color : DataPortColors.Disabled;

        var (isConnectedToInputConnectors, isConnectedToOuputConnectors) = DataPortChildNodeModelExtensions.GetEffectiveIconFill(
            dataPortDirection,
            childNode.LinkDirections,
            hasInboundLink,
            hasOutboundLink);

        return ColoredIconFactory.GetDataPortIcon(color, dataPortDirection, isConnectedToOuputConnectors, isConnectedToInputConnectors, size);
    }

    internal static string? GetSvgIcon(Tree.Builder.TreeBuilder treeBuilder, string iconName)
        => treeBuilder.GetSvgIcon(iconName);

    private static bool ShouldSend(DataPortChildNodeModel node)
    {
        var transferMode = node.GetSystemProperty<DataPortTransferMode>();

        if (transferMode is null || transferMode.AvailableValues is [] or [DataPortTransferMode.None])
        {
            // A node with no transfer mode of its own (e.g. an envelope child, whose mode is
            // always fixed to None) carries no meaning; its switched-off state instead follows
            // its parent datapoint. A node that offers exactly one mode it can actually send in
            // does carry meaning, and is answered for below.
            return node.Parent is DataPortChildNodeModel parentChild && ShouldSend(parentChild);
        }

        return transferMode.TypedValue is not DataPortTransferMode.None;
    }
}
