using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ViciOne.Tree.Builder.NodeTypes;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class AddDataPortMenuItemProvider(
    IRulesetProvider rulesetProvider,
    DataPortTreeBuilderRegistry registry,
    DataPortTreeState treeState)
{
    private DataPortContextMenuItem CreateMenuItem(string rulesetKey, Root root)
    {
        var rootNode = treeState.GetDataPortRootNode(root.Id);
        if (rootNode is null)
            return new DataPortContextMenuItem(root.Name, true, rulesetKey);

        rootNode.PossibleChildren = [.. rootNode.GetPossibleChildNodes()];

        return new DataPortContextMenuItem(root.Name, rootNode.PossibleChildren.Any(), rulesetKey);
    }

    /// <summary>
    /// The DataPort types the user may add. A ruleset that failed validation is left out: it has no
    /// tree builder, so the section could not show a tree for it anyway. A ruleset the provider
    /// gained since the registry was initialised is built here on first sight, so it is offered
    /// without the section having to be re-created.
    /// </summary>
    public IEnumerable<DataPortContextMenuItem> GetMenuItems()
        => [.. rulesetProvider.GetRulesetIdentifiers(DataPortTreeBuilderRegistry.DataPortCategory)
            .Where(registry.CanProvideTreeBuilder)
            .Select(rulesetId => (rulesetId.Key, Ruleset: rulesetProvider.GetRuleset(rulesetId)))
            .Where(candidate => candidate.Ruleset?.Root is not null)
            .Select(candidate => CreateMenuItem(candidate.Key, candidate.Ruleset.Root!))
            .OrderBy(item => item.DisplayText)];
}
