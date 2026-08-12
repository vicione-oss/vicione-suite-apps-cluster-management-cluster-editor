using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.TreeEditor.Builder.Interface.Icons;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

internal sealed class DataPortIconResolver(IDatastore datastore)
{
    public IEnumerable<IIcon> GetIcons(ITreeNode node)
    {
        if (node is not DataPortNodeModel dataPortNode)
            yield break;

        if (dataPortNode is DataPortRootNodeModel rootNodeModel)
        {
            var iconName = string.IsNullOrEmpty(dataPortNode.Icon)
                ? (dataPortNode.AvailableIcons.Count > 0 ? dataPortNode.AvailableIcons[0] : null)
                : dataPortNode.Icon;
            if (!string.IsNullOrEmpty(iconName))
            {
                var icon = DataPortTreeIconProvider.GetSvgIcon(rootNodeModel.Builder, iconName) ?? DataPortTreeIconProvider.GetSvgIcon(rootNodeModel.Builder, "server");
                if (!string.IsNullOrEmpty(icon))
                    yield return new SvgIcon(icon);
            }

            yield break;
        }

        if (dataPortNode is DataPortChildNodeModel { NodeReference: not null } childNode)
        {
            switch (childNode.Icon)
            {
                case null:
                    {
                        var nodeType = childNode.GetRootNode().Builder.NodeTypes[childNode.NodeReference!.Id];
                        var iconName = nodeType.Icons.FirstOrDefault();
                        if (iconName is not null)
                        {
                            var icon = DataPortTreeIconProvider.GetSvgIcon(childNode.RootNode.Builder, iconName);
                            if (!string.IsNullOrEmpty(icon))
                                yield return new SvgIcon(icon);
                        }
                        break;
                    }

                case not null when childNode.Icon.Equals("datapoint", StringComparison.OrdinalIgnoreCase):
                    {
                        var icon = DataPortTreeIconProvider.GetDataPointIcon(childNode, 24, datastore.Builder.Cache);
                        if (icon is not null)
                            yield return icon;
                        break;
                    }

                default:
                    {
                        var icon = DataPortTreeIconProvider.GetSvgIcon(childNode.RootNode.Builder, childNode.Icon);
                        if (!string.IsNullOrEmpty(icon))
                            yield return new SvgIcon(icon);
                        break;
                    }
            }
        }
    }
}
