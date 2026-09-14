using System.IO;
using System.Reflection;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Serialization.Json;
using ViciOne.Tree.Builder.Rules;
using ViciOne.Tree.Builder.Rules.Yaml;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Resources;

internal static class TestResources
{
    private static readonly string s_assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;

    internal static Ruleset EnvelopeChildrenEdgeCasesRuleset => LoadRuleSet("EnvelopeChildrenEdgeCases.yaml");
    internal static Ruleset MqttEnvelopeChildrenRuleset => LoadRuleSet("MqttEnvelopeChildren.yaml");
    internal static Ruleset MqttRuleset => LoadRuleSet("MQTT.yaml");
    internal static Ruleset SqliteRuleset => LoadRuleSet("SQLite.yaml");

    internal static IFunctionBlockDesign LoadFbDesign()
    {
        Assert.NotNull(s_assemblyDirectory);

        var fbDesignFile = Path.Combine(s_assemblyDirectory, "Resources", "TestFbDesign.json");
        var fbDesignJson = File.ReadAllText(fbDesignFile);
        return JsonSerialization.Default.Load<FunctionBlockDesign>(fbDesignJson);
    }

    private static Ruleset LoadRuleSet(string yaml)
    {
        Assert.NotNull(s_assemblyDirectory);

        var rulesetFile = Path.Combine(s_assemblyDirectory, "Resources", yaml);
        return RulesDeserializer.Deserialize(rulesetFile);
    }
}
