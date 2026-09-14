using System;
using System.Text;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.ContextMenu;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

/// <summary>
/// The icon the insert menu shows for a node that does not exist yet. It answers by building the
/// node the entry would create and asking <see cref="DataPortTreeIconProvider"/> for its icon, so
/// that picking an entry never changes the icon the user just looked at.
/// </summary>
internal static class DataPortMenuIconProvider
{
    private const string DataPointIconName = "datapoint";

    internal static string? GetIcon(DataPortChildNodeContextMenuDescriptor descriptor)
    {
        var builder = descriptor.ParentNode.GetRootNode().Builder;

        // A descriptor that only groups the entries of a namespace carries no node type of its own.
        if (!builder.NodeTypes.ContainsKey(descriptor.NodeReference.Id))
            return null;

        if (!DataPointIconName.Equals(descriptor.IconName, StringComparison.OrdinalIgnoreCase))
            return builder.GetSvgIcon(descriptor.IconName);

        // Nothing is linked before the node exists. A marker envelope child carries the datapoint
        // icon name but has no value to colour it by, and falls back to the plain node type icon -
        // the same fallback DataPortIconResolver makes for a node already in the tree.
        var node = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(descriptor, descriptor.ParentNode);
        return DataPortTreeIconProvider.GetDataPointIconMarkup(node, size: null, hasInboundLink: false, hasOutboundLink: false)
            ?? builder.GetSvgIcon(descriptor.IconName);
    }

    internal static string GetIconData(DataPortChildNodeContextMenuDescriptor descriptor)
    {
        var icon = GetIcon(descriptor);
        if (string.IsNullOrEmpty(icon))
            return string.Empty;

        icon = icon.Replace("viewBox=\"0 0 32 32\"", "viewBox=\"4 4 28 28\" width=\"16\" height=\"16\"", StringComparison.InvariantCulture);
        icon = icon.Replace("currentColor", DataPortColorConstants.ColorEditorFont, StringComparison.InvariantCulture);

        return $"data:image/svg+xml;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(icon))}";
    }
}
