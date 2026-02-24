using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.Core.Dataflow.DataModel;

namespace Shared.Designs;

public class ClusterDependencyResolver(IEnumerable<ClusterDependencyPackage>? designs = null) : IDependencyResolver
{
    private readonly List<ClusterDependencyPackage> _designPackages = designs?.ToList() ?? [];

    /// <summary>
    /// Instead of the Root.Id it seems that the Id of the first child is used
    /// e.g. Root.Id => MQTTDataPort but MQTT-Broker is searched for
    /// </summary>
    /// <param name="designId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public ClusterDependency ResolveDataPortDesignDependency(string designId)
    {
        var package = _designPackages.FirstOrDefault(d => d.DataPortRootChildNodeIds?.Contains(designId) == true);

        return package is not null
            ? package.Dependency
            : throw new InvalidOperationException($"Unknown dependency for DataPort {designId}");
    }

    public IEnumerable<FunctionBlockDesign> ResolveDependency(ClusterDependency dependency)
    {
        var designDependency = _designPackages.FirstOrDefault(d => d.Dependency == dependency);

        return designDependency is null
            ? throw new InvalidOperationException($"Unknown dependency for {dependency.Name} {dependency.Version}")
            : (IEnumerable<FunctionBlockDesign>)designDependency.FunctionBlockDesigns;
    }

    public FunctionBlockDesign ResolveFunctionBlockDesign(Guid designId)
        => _designPackages.SelectMany(k => k.FunctionBlockDesigns).FirstOrDefault(k => k.Id == designId)
            ?? throw new InvalidOperationException($"Failed to resolve FunctionBlock design for id '{designId}'");

    public ClusterDependency ResolveFunctionBlockDesignDependency(Guid designId)
    {
        var designDependency = _designPackages.FirstOrDefault(k => k.FunctionBlockDesigns.Any(d => d.Id == designId))
            ?? throw new InvalidOperationException($"Failed to resolve dependency for FunctionBlock design '{designId}'");

        return designDependency.Dependency;
    }
}
