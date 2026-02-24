using ViciOne.Cluster.Builder;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.TreeEditor.Builder.Interface.Icons;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

internal interface IDataPortTreeIconProvider
{
    IIcon? GetDataPointIcon(DataPortChildNodeModel childNode, int size, IClusterCache clusterCache);

    string? GetIconMarkupString(string iconName);
}
