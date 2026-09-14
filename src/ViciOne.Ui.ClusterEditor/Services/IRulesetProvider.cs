using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Tree.Builder.Rules;

namespace ViciOne.Ui.ClusterEditor.Services;

public interface IRulesetProvider
{
    ClusterDependency GetClusterDependency(string dataPortType);
    Ruleset GetRuleset(RulesetIdentifier rulesetIdentifier);
    IEnumerable<RulesetIdentifier> GetRulesetIdentifiers(string category);
    ClusterDependency GetSystemDataPortDependency();
}

public record RulesetIdentifier(string Category, string Key);
