using ViciOne.Cluster.Model;
using ViciOne.Tree.Builder.Rules;

namespace Shared.Designs;

public interface IPackagesStore
{
    bool TryAddPackage(ClusterDependency dependency, string directory, bool hide = false);
    bool TryAddPackage(ClusterDependency dependency, IReadOnlyCollection<Type> types, IReadOnlyCollection<Ruleset> rulesets, bool hide = false);
}
