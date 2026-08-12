using NSubstitute;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

public sealed class DataPortTreeBuilderRegistryTests
{
    private readonly DataPortTreeBuilderRegistry _registry;
    private readonly IRulesetProvider _rulesetProvider;

    public DataPortTreeBuilderRegistryTests()
    {
        _rulesetProvider = Substitute.For<IRulesetProvider>();
        _rulesetProvider.GetRuleset(Arg.Any<RulesetIdentifier>()).Returns(Resources.TestResources.MqttRuleset);
        _registry = new DataPortTreeBuilderRegistry(_rulesetProvider);
    }

    [Fact]
    public void GetOrCreateTreeBuilder_CalledTwiceWithSameRuleset_ReturnsCachedInstance()
    {
        // Act
        var first = _registry.GetOrCreateTreeBuilder(DataPortTreeBuilderRegistry.DataPortCategory, "mqtt");
        var second = _registry.GetOrCreateTreeBuilder(DataPortTreeBuilderRegistry.DataPortCategory, "mqtt");

        // Assert
        Assert.Same(first, second);
        _rulesetProvider.Received(1).GetRuleset(Arg.Any<RulesetIdentifier>());
    }

    [Fact]
    public void Initialize_CreatesTreeBuildersForAllRulesetIdentifiers()
    {
        // Arrange
        _rulesetProvider.GetRulesetIdentifiers(DataPortTreeBuilderRegistry.DataPortCategory)
            .Returns([new RulesetIdentifier(DataPortTreeBuilderRegistry.DataPortCategory, "mqtt")]);

        // Act
        _registry.Initialize();

        // Assert
        var rulesetRootId = Resources.TestResources.MqttRuleset.Root!.Id;
        Assert.True(_registry.TryGetTreeBuilderForDataPort(rulesetRootId, out var treeBuilder));
        Assert.NotNull(treeBuilder);
    }

    [Fact]
    public void TryGetTreeBuilderForDataPort_WhenNotRegistered_ReturnsFalse()
    {
        // Act
        var result = _registry.TryGetTreeBuilderForDataPort("unknown-ruleset", out var treeBuilder);

        // Assert
        Assert.False(result);
        Assert.Null(treeBuilder);
    }

    [Fact]
    public void TryGetTreeBuilderForDataPort_WhenRegistered_ReturnsTrue()
    {
        // Arrange
        _registry.GetOrCreateTreeBuilder(DataPortTreeBuilderRegistry.DataPortCategory, "mqtt");
        var rulesetRootId = Resources.TestResources.MqttRuleset.Root!.Id;

        // Act
        var result = _registry.TryGetTreeBuilderForDataPort(rulesetRootId, out var treeBuilder);

        // Assert
        Assert.True(result);
        Assert.NotNull(treeBuilder);
    }
}
