using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Sections.Topology.Extensions;

internal static class EngineHostExtensions
{
    public static void Apply(this EngineHost target, EngineHostEditor editor, EngineHost source)
    {
        if (!string.IsNullOrEmpty(source.Description))
            editor.SetDescription(target, source.Description);
        else
            editor.ResetDescription(target);

        editor.SetLogLevel(target, source.LogLevel);

        if (!string.IsNullOrEmpty(source.Name))
            editor.SetName(target, source.Name);

        if (!string.IsNullOrEmpty(source.Icon))
            editor.SetIcon(target, source.Icon);
        else
            editor.ResetIcon(target);

        editor.SetReplicas(target, source.Replicas);
    }

    public static EngineHost ShallowCopy(this EngineHost original) => new()
    {
        Description = original.Description,
        Icon = original.Icon,
        Id = original.Id,
        LogLevel = original.LogLevel,
        Name = original.Name,
        Replicas = original.Replicas,
    };
}
