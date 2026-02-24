using ViciOne.TreeBuilder.Rules;
using ViciOne.TreeBuilder.Rules.Yaml;

namespace Shared.Designs;

public static class RulesetLoader
{
    public static IReadOnlyDictionary<string, Ruleset> LoadRulesets(string directory)
    {
        var rulesetFiles = Directory.GetFiles(directory, "*.yaml", SearchOption.TopDirectoryOnly);
        Dictionary<string, Ruleset> rulesets = [];

        foreach (var file in rulesetFiles)
        {
            var ruleset = RulesDeserializer.Deserialize(file);
            if (ruleset.Root is null)
                throw new InvalidOperationException($"Ruleset from '{file}' does not contain a valid root.");
            rulesets.Add(ruleset.Root.Id, ruleset);
        }

        return rulesets;
    }
}
