using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

internal sealed class DataPortTreeBuilderRegistry(IRulesetProvider rulesetProvider)
{
    public const string DataPortCategory = "DataPorts";
    private readonly Dictionary<string, TreeBuilder.TreeBuilder> _treeBuilders = [];

    private TreeBuilder.TreeBuilder GetOrCreateTreeBuilder(RulesetIdentifier rulesetId)
    {
        if (!_treeBuilders.TryGetValue(rulesetId.Key, out var treeBuilder))
        {
            treeBuilder = new TreeBuilder.TreeBuilder(rulesetProvider.GetRuleset(rulesetId));
            _treeBuilders.Add(rulesetId.Key, treeBuilder);
        }

        return treeBuilder;
    }

    public TreeBuilder.TreeBuilder GetOrCreateTreeBuilder(string dataPortCategory, string rulesetIdentifier)
        => GetOrCreateTreeBuilder(new RulesetIdentifier(dataPortCategory, rulesetIdentifier));

    public void Initialize()
    {
        _treeBuilders.Clear();

        // Create a treebuilder for each ruleset
        foreach (var rulesetId in rulesetProvider.GetRulesetIdentifiers(DataPortCategory).ToArray())
            GetOrCreateTreeBuilder(rulesetId);
    }

    public bool TryGetTreeBuilderForDataPort(string rulesetId, out TreeBuilder.TreeBuilder? treeBuilder)
    {
        treeBuilder = _treeBuilders.Values.FirstOrDefault(k => rulesetId.Equals(k.Ruleset.Root?.Id, StringComparison.OrdinalIgnoreCase));
        return treeBuilder is not null;
    }
}
