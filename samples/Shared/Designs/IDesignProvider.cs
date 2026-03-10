using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Core.Dataflow.DataModel;

namespace Shared.Designs;

public interface IDesignProvider
{
    IDependencyResolver CreateResolver();

    Task<IReadOnlyCollection<ClusterDependency>> GetClusterDependencies(bool includeDataPortDependencies = false);

    /// <summary>
    /// returns FunctionBlockDesign ids provided by given cluster dependencies
    /// </summary>
    /// <param name="clusterDependencies"></param>
    /// <returns></returns>
    Task<IReadOnlyCollection<Guid>> GetFunctionBlockDesignIds(params IReadOnlyCollection<ClusterDependency>? clusterDependencies);

    Task<IReadOnlyCollection<FunctionBlockDesign>> GetFunctionBlockDesigns(params IReadOnlyCollection<ClusterDependency>? clusterDependencies);
}
