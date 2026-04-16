using System.Diagnostics.CodeAnalysis;
using Shared.Designs;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Core.Dataflow.DataModel;

namespace Server.Designs;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used in testing")]
internal sealed partial class InMemoryPackagesStore : IDesignProvider
{
    public IDependencyResolver CreateResolver()
        => new ClusterDependencyResolver(GetDependencyPackages());

    public Task<IReadOnlyCollection<ClusterDependency>> GetClusterDependencies(bool includeDataPortDependencies = false)
    {
        if (includeDataPortDependencies)
            return Task.FromResult<IReadOnlyCollection<ClusterDependency>>(_packages.Keys);
        return Task.FromResult<IReadOnlyCollection<ClusterDependency>>([.. _packages.Where(p => !p.Value.Rulesets.Any()).Select(p => p.Key)]);
    }

    private ClusterDependencyPackage[] GetDependencyPackages()
    {
        Dictionary<ClusterDependency, ClusterDependencyPackage> packages = new(_packages.Count);

        foreach (var (dependency, storeItem) in _packages)
        {
            if (packages.TryGetValue(dependency, out var package))
            {
                package.FunctionBlockDesigns.AddRange(storeItem.Designs.Values);
                package.DataPortRootChildNodeIds.AddRange(storeItem.Rulesets.Values
                    .Where(r => r.Root is not null)
                    .SelectMany(r => r.Root!.ChildNodes.Select(n => n.Id)));
            }
            else
            {
                packages[dependency] = new(dependency, [.. storeItem.Designs.Values], [..
                    storeItem.Rulesets.Values
                    .Where(r => r.Root is not null)
                    .SelectMany(r => r.Root!.ChildNodes.Select(n => n.Id))]);
            }
        }

        return [.. packages.Values];
    }

    public Task<IReadOnlyCollection<Guid>> GetFunctionBlockDesignIds(params IReadOnlyCollection<ClusterDependency>? clusterDependencies)
        => Task.FromResult<IReadOnlyCollection<Guid>>([.. GetFunctionBlockDesignsInternal(clusterDependencies).Select(d => d.Id)]);

    public Task<IReadOnlyCollection<FunctionBlockDesign>> GetFunctionBlockDesigns(params IReadOnlyCollection<ClusterDependency>? clusterDependencies)
        => Task.FromResult(GetFunctionBlockDesignsInternal(clusterDependencies));

    private IEnumerable<FunctionBlockDesign> GetFunctionBlockDesigns(ClusterDependency clusterDependency)
    {
        if (_packages.TryGetValue(clusterDependency, out var item))
        {
            if (item.Hide)
            {
                LogHiddenDependency(logger, clusterDependency.Name, clusterDependency.Version);
                yield break;
            }

            foreach (var design in item.Designs.Values)
                yield return design;
            yield break;
        }

        LogMissingDependency(logger, clusterDependency.Name, clusterDependency.Version);
    }

    private IReadOnlyCollection<FunctionBlockDesign> GetFunctionBlockDesigns()
        => [.. _packages.Values.Where(p => !p.Hide).SelectMany(v => v.Designs.Values)];

    private IReadOnlyCollection<FunctionBlockDesign> GetFunctionBlockDesignsInternal(params IReadOnlyCollection<ClusterDependency>? clusterDependencies)
    {
        if (clusterDependencies is null)
            return GetFunctionBlockDesigns();

        if (clusterDependencies.Count == 0)
            return [];

        List<FunctionBlockDesign> requestedDesigns = [];

        foreach (var clusterDependency in clusterDependencies)
            requestedDesigns.AddRange(GetFunctionBlockDesigns(clusterDependency));

        return requestedDesigns;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cluster dependency {Name} version {Version} is hidden.")]
    private static partial void LogHiddenDependency(ILogger logger, string name, string version);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cluster dependency {Name} version {Version} can't be found.")]
    private static partial void LogMissingDependency(ILogger logger, string name, string version);
}
