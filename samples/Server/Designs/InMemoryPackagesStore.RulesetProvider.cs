using Shared.Designs;
using ViciOne.Cluster.Model;
using ViciOne.Tree.Builder.Rules;
using ViciOne.Ui.ClusterEditor.Services;

namespace Server.Designs;

internal sealed partial class InMemoryPackagesStore : IRulesetProvider, IRulesetSource
{
    private ClusterDependency? _systemDataPortDependency;

    public IEnumerable<Ruleset> GetAllRulesets()
        => _packages.Values.SelectMany(k => k.Rulesets.Values);

    public ClusterDependency GetClusterDependency(string dataPortType)
    {
        foreach (var entry in _packages)
        {
            if (entry.Value.Rulesets.ContainsKey(dataPortType))
                return entry.Key;
        }

        throw new InvalidOperationException($"Can't find ClusterDependency for DataPort type:{dataPortType}");
    }

    public Ruleset GetRuleset(string category, string rulesetId)
    {
        foreach (var entry in _packages)
        {
            if (entry.Value.Rulesets.ContainsKey(rulesetId))
                return entry.Value.Rulesets[rulesetId];
        }
        throw new InvalidOperationException($"Can't find Ruleset for category:{category} and rulesetId:{rulesetId}");
    }

    public Ruleset GetRuleset(RulesetIdentifier rulesetIdentifier)
        => GetRuleset(rulesetIdentifier.Category, rulesetIdentifier.Key);

    public IEnumerable<RulesetIdentifier> GetRulesetIdentifiers(string category)
        => _packages.Values
            .SelectMany(k => k.Rulesets.Keys)
            .Select(key => new RulesetIdentifier(category, key));

    public ClusterDependency GetSystemDataPortDependency()
        => _systemDataPortDependency ?? throw new InvalidOperationException($"Use {nameof(SetSystemDataPortDependency)} before adding rulesets");

    public void SetSystemDataPortDependency(ClusterDependency clusterDependency)
        => _systemDataPortDependency = clusterDependency;
}
