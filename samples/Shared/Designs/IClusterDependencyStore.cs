using ViciOne.Cluster.Model;

namespace Shared.Designs;

public interface IClusterDependencyStore
{
    Task<IReadOnlyCollection<ClusterDependency>> LoadDependencies(CancellationToken cancellationToken);
}
