using System.Linq;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using ViciOne.Tree.Builder.Rules;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

public sealed class AddDataPortMenuItemProviderTests
{
    private readonly AddDataPortMenuItemProvider _provider;
    private readonly DataPortTreeBuilderRegistry _registry;
    private readonly IRulesetProvider _rulesetProvider = Substitute.For<IRulesetProvider>();

    public AddDataPortMenuItemProviderTests()
    {
        _registry = new DataPortTreeBuilderRegistry(_rulesetProvider, new FakeLogger<DataPortTreeBuilderRegistry>());
        _provider = new AddDataPortMenuItemProvider(_rulesetProvider, _registry, new DataPortTreeState());

        _rulesetProvider.GetRulesetIdentifiers(DataPortTreeBuilderRegistry.DataPortCategory).Returns(
        [
            new RulesetIdentifier(DataPortTreeBuilderRegistry.DataPortCategory, "broken"),
            new RulesetIdentifier(DataPortTreeBuilderRegistry.DataPortCategory, "mqtt"),
        ]);
        _rulesetProvider.GetRuleset(new RulesetIdentifier(DataPortTreeBuilderRegistry.DataPortCategory, "mqtt"))
            .Returns(Resources.TestResources.MqttRuleset);

        // A ruleset that declares a Root but breaks another rule still reaches the menu candidates,
        // and would throw again if it were offered and picked.
        _rulesetProvider.GetRuleset(new RulesetIdentifier(DataPortTreeBuilderRegistry.DataPortCategory, "broken"))
            .Returns(new Ruleset { Root = new() { Id = "BrokenDataPort", Name = "Broken" } });

        _registry.Initialize();
    }

    [Fact]
    public void Leaves_out_a_ruleset_that_failed_validation()
    {
        // Act
        var identifiers = _provider.GetMenuItems().Select(item => item.Identifier);

        // Assert
        identifiers.Should().NotContain("broken");
    }

    [Fact]
    public void Offers_a_ruleset_that_was_built()
    {
        // Act
        var identifiers = _provider.GetMenuItems().Select(item => item.Identifier);

        // Assert
        identifiers.Should().ContainSingle().Which.Should().Be("mqtt");
    }

    [Fact]
    public void Enables_a_DataPort_type_that_has_no_root_node_yet()
    {
        // Act
        var menuItems = _provider.GetMenuItems();

        // Assert
        menuItems.Should().AllSatisfy(item => item.Enabled.Should().BeTrue());
    }

    [Fact]
    public void Offers_a_ruleset_the_provider_gained_after_the_registry_was_initialised()
    {
        // Arrange
        var edgeCasesRulesetId = new RulesetIdentifier(DataPortTreeBuilderRegistry.DataPortCategory, "edge-cases");
        _rulesetProvider.GetRulesetIdentifiers(DataPortTreeBuilderRegistry.DataPortCategory).Returns(
        [
            new RulesetIdentifier(DataPortTreeBuilderRegistry.DataPortCategory, "mqtt"),
            edgeCasesRulesetId,
        ]);
        _rulesetProvider.GetRuleset(edgeCasesRulesetId).Returns(Resources.TestResources.EnvelopeChildrenEdgeCasesRuleset);

        // Act
        var identifiers = _provider.GetMenuItems().Select(item => item.Identifier);

        // Assert
        identifiers.Should().BeEquivalentTo(["mqtt", "edge-cases"]);
    }
}
