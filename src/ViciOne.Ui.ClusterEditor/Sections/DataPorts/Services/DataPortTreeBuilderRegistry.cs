using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Logging;
using ViciOne.Tree.Builder;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed partial class DataPortTreeBuilderRegistry(IRulesetProvider rulesetProvider, ILogger<DataPortTreeBuilderRegistry> logger)
{
    public const string DataPortCategory = "DataPorts";
    private readonly HashSet<string> _failedRulesets = [];
    private readonly Dictionary<string, Tree.Builder.TreeBuilder> _treeBuilders = [];

    [LoggerMessage(Level = LogLevel.Error, Message = "Skipping ruleset {RulesetKey}: {ValidationMessages}")]
    public static partial void CreateTreeBuilderFailed(ILogger logger, Exception ex, string rulesetKey, string validationMessages);

    private Tree.Builder.TreeBuilder GetOrCreateTreeBuilder(RulesetIdentifier rulesetId)
    {
        if (!_treeBuilders.TryGetValue(rulesetId.Key, out var treeBuilder))
        {
            treeBuilder = new Tree.Builder.TreeBuilder(rulesetProvider.GetRuleset(rulesetId));
            _treeBuilders.Add(rulesetId.Key, treeBuilder);
        }

        return treeBuilder;
    }

    public Tree.Builder.TreeBuilder GetOrCreateTreeBuilder(string dataPortCategory, string rulesetIdentifier)
        => GetOrCreateTreeBuilder(new RulesetIdentifier(dataPortCategory, rulesetIdentifier));

    /// <summary>
    /// Whether a tree builder exists or can be built for this ruleset. A ruleset the registry has
    /// not seen yet, e.g. one the provider gained after <see cref="Initialize"/>, is built on first
    /// request. A ruleset that failed validation must not be offered anywhere, since building it
    /// again would only fail again, so the failure is remembered until the next
    /// <see cref="Initialize"/>.
    /// </summary>
    public bool CanProvideTreeBuilder(RulesetIdentifier rulesetId)
    {
        if (_treeBuilders.ContainsKey(rulesetId.Key))
            return true;
        if (_failedRulesets.Contains(rulesetId.Key))
            return false;

        return TryCreateTreeBuilder(rulesetId);
    }

    public void Initialize()
    {
        _treeBuilders.Clear();
        _failedRulesets.Clear();

        // Create a treebuilder for each ruleset
        foreach (var rulesetId in rulesetProvider.GetRulesetIdentifiers(DataPortCategory).ToArray())
            TryCreateTreeBuilder(rulesetId);
    }

    private bool TryCreateTreeBuilder(RulesetIdentifier rulesetId)
    {
        try
        {
            GetOrCreateTreeBuilder(rulesetId);
            return true;
        }
        catch (RulesetValidationFailedException ex)
        {
            _failedRulesets.Add(rulesetId.Key);
            CreateTreeBuilderFailed(logger, ex, rulesetId.Key, string.Join("; ", ex.Messages));
            return false;
        }
    }

    public bool TryGetTreeBuilderForDataPort(string rulesetId, out Tree.Builder.TreeBuilder? treeBuilder)
    {
        treeBuilder = _treeBuilders.Values.FirstOrDefault(k => rulesetId.Equals(k.Ruleset.Root?.Id, StringComparison.OrdinalIgnoreCase));
        return treeBuilder is not null;
    }
}
