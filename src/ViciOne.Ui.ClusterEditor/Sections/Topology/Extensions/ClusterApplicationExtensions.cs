using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Sections.Topology.Extensions;

internal static class ClusterApplicationExtensions
{
    public static void Apply(this ClusterApplication target, IClusterApplicationEditor editor, ClusterApplication source)
    {
        if (!string.IsNullOrEmpty(source.Description))
            editor.SetDescription(target, source.Description);
        else
            editor.ResetDescription(target);

        if (!string.IsNullOrEmpty(source.Name))
            editor.SetName(target, source.Name);
    }

    public static ClusterApplication ShallowCopy(this ClusterApplication original)
        => new()
        {
            AutoScale = original.AutoScale,
            Description = original.Description,
            Id = original.Id,
            Name = original.Name,
            Replicas = original.Replicas,
            Type = original.Type,
        };
}
