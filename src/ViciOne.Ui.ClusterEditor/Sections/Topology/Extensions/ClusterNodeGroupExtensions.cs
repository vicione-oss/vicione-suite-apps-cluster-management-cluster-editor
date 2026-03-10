using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Sections.Topology.Extensions;

public static class ClusterNodeGroupExtensions
{
    public static void Apply(this ClusterNodeGroup target, IClusterNodeGroupEditor editor, ClusterNodeGroup source)
    {
        if (!string.IsNullOrEmpty(source.Description))
            editor.SetDescription(target, source.Description);
        else
            editor.ResetDescription(target);

        if (!string.IsNullOrEmpty(source.Name))
            editor.SetName(target, source.Name);

        if (!string.IsNullOrEmpty(source.Icon))
            editor.SetIcon(target, source.Icon);
        else
            editor.ResetIcon(target);

        editor.SetNetworkType(target, source.NetworkType);
    }

    public static ClusterNodeGroup ShallowCopy(this ClusterNodeGroup original) => new()
    {
        Description = original.Description,
        Icon = original.Icon,
        Id = original.Id,
        Name = original.Name,
        NetworkType = original.NetworkType,
    };
}
