using ViciOne.Cluster.Builder;

namespace ViciOne.Ui.ClusterEditor.Sections.Topology.Extensions;

internal static class EngineExtensions
{
    public static void Apply(this Cluster.Model.Engine target, EngineEditor editor, Cluster.Model.Engine source)
    {
        editor.SetEnabled(target, source.Enabled);
        editor.SetEngineType(target, source.EngineType);

        if (!string.IsNullOrEmpty(source.Description))
            editor.SetDescription(target, source.Description);
        else
            editor.ResetDescription(target);

        if (!string.IsNullOrEmpty(source.Icon))
            editor.SetIcon(target, source.Icon);
        else
            editor.ResetIcon(target);

        if (!string.IsNullOrEmpty(source.Name))
            editor.SetName(target, source.Name);

        editor.SetLogLevel(target, source.LogLevel);
        editor.SetRunIndex(target, source.RunIndex);
        editor.SetMaxCycleTime(target, source.MaxCycleTime);
        editor.SetMinCycleTime(target, source.MinCycleTime);
    }

    public static Cluster.Model.Engine ShallowCopy(this Cluster.Model.Engine original) => new()
    {
        Description = original.Description,
        Enabled = original.Enabled,
        EngineType = original.EngineType,
        Icon = original.Icon,
        Id = original.Id,
        LogLevel = original.LogLevel,
        MaxCycleTime = original.MaxCycleTime,
        MinCycleTime = original.MinCycleTime,
        Name = original.Name,
        RunIndex = original.RunIndex,
    };
}
