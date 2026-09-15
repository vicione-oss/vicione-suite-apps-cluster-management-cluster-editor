using ViciOne.Cluster.Model;
using ViciOne.Tree.Builder.Rules;

namespace Shared.Designs;

/// <summary>
/// Read access to the rulesets the sample app has loaded. This is the sample app's own view on the
/// rulesets it also hands to the cluster editor, so that features such as the debug generator do not
/// have to go through the editor to reach them.
/// </summary>
public interface IRulesetSource
{
    IEnumerable<Ruleset> GetAllRulesets();

    ClusterDependency GetSystemDataPortDependency();
}
