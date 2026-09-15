using Shared.Designs;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Tree.Builder.NodeTypes;

namespace Shared.Debugging.Extensions;

internal static class ClusterBuilderDebugExtensions
{
    /// <summary>
    /// Adds the system data port dependency to the cluster unless it is already there. Creating a data port
    /// requires it, independent of the data port type.
    /// </summary>
    public static void EnsureSystemDataPortDependencyExists(this IClusterBuilder builder, IRulesetSource rulesetSource)
    {
        var systemDataPortDependency = rulesetSource.GetSystemDataPortDependency();
        if (!builder.Cache.ClusterDependencies.Contains(systemDataPortDependency))
            builder.Editors.Cluster.AddDependency(systemDataPortDependency.Name, systemDataPortDependency.Version);
    }

    /// <summary>
    /// Engines that are not referenced by any data port or function block.
    /// </summary>
    public static IEnumerable<Engine> GetUnusedEngines(this IClusterCache cache)
        => cache.EngineIds.Values.Where(engine =>
            !cache.GetDataPorts(engine).Any() &&
            !cache.GetFunctionBlocks(engine).Any());

    /// <summary>
    /// Engines that are referenced by an element of the given dataflow. An engine can only be used by a
    /// single dataflow, so the first match already decides it.
    /// </summary>
    public static IEnumerable<Engine> GetUsedEngines(this IClusterCache cache, Dataflow dataflow)
        => cache.EngineIds.Values.Where(engine =>
            cache.GetDataPorts(engine).Any(dataPort => dataPort.Dataflow == dataflow) ||
            cache.GetFunctionBlocks(engine).Any(functionBlock => cache.GetDataflow(functionBlock) == dataflow));

    /// <summary>
    /// A tree node type is a data point when it carries at least one data type.
    /// </summary>
    public static bool IsDataPoint(this NodeType nodeType)
        => nodeType.DataTypes.Length > 0;
}
