using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Sections.Topology.Extensions;

public static class ClusterNodeExtensions
{
    public static void Apply(this ClusterNode target, ClusterNodeEditor editor, ClusterNode source)
    {
        editor.SetComputePowerLevel(target, source.ComputePowerLevel);

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

        editor.SetType(target, source.Type);
    }

    public static ClusterNode ShallowCopy(this ClusterNode original) => new()
    {
        ComputePowerLevel = original.ComputePowerLevel,
        Description = original.Description,
        Icon = original.Icon,
        Id = original.Id,
        Name = original.Name,
        Type = original.Type,
    };
}
