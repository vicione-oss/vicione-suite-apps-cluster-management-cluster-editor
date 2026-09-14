using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using ViciOne.Tree.Builder.Rules;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

public sealed class DataPortTreeBuilderRegistryTests
{
    private readonly FakeLogger<DataPortTreeBuilderRegistry> _logger = new();
    private readonly DataPortTreeBuilderRegistry _registry;
    private readonly IRulesetProvider _rulesetProvider;

    public DataPortTreeBuilderRegistryTests()
    {
        _rulesetProvider = Substitute.For<IRulesetProvider>();
        _rulesetProvider.GetRuleset(Arg.Any<RulesetIdentifier>()).Returns(Resources.TestResources.MqttRuleset);
        _registry = new DataPortTreeBuilderRegistry(_rulesetProvider, _logger);
    }

    private void GivenAnInvalidRulesetBeforeTheMqttOne()
    {
        _rulesetProvider.GetRulesetIdentifiers(DataPortTreeBuilderRegistry.DataPortCategory).Returns(
        [
            new RulesetIdentifier(DataPortTreeBuilderRegistry.DataPortCategory, "broken"),
            new RulesetIdentifier(DataPortTreeBuilderRegistry.DataPortCategory, "mqtt"),
        ]);

        // A ruleset without Common and Root fails validation in the TreeBuilder constructor.
        _rulesetProvider.GetRuleset(new RulesetIdentifier(DataPortTreeBuilderRegistry.DataPortCategory, "broken"))
            .Returns(new Ruleset());
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
    public void Initialize_WhenARulesetFailsValidation_KeepsTheRemainingRulesets()
    {
        // Arrange
        GivenAnInvalidRulesetBeforeTheMqttOne();

        // Act
        var initialize = _registry.Initialize;

        // Assert
        initialize.Should().NotThrow();
        Assert.True(_registry.TryGetTreeBuilderForDataPort(Resources.TestResources.MqttRuleset.Root!.Id, out var treeBuilder));
        Assert.NotNull(treeBuilder);
    }

    [Fact]
    public void Initialize_WhenARulesetFailsValidation_ReportsTheKeyAndTheValidationMessages()
    {
        // Arrange
        GivenAnInvalidRulesetBeforeTheMqttOne();

        // Act
        _registry.Initialize();

        // Assert
        _logger.LatestRecord.Level.Should().Be(LogLevel.Error);
        _logger.LatestRecord.Message.Should().Match("Skipping ruleset broken: ?*");
    }

    [Fact]
    public void CanProvideTreeBuilder_WhenTheRulesetFailedValidation_ReturnsFalse()
    {
        // Arrange
        GivenAnInvalidRulesetBeforeTheMqttOne();
        _registry.Initialize();

        // Act
        var result = _registry.CanProvideTreeBuilder(new RulesetIdentifier(DataPortTreeBuilderRegistry.DataPortCategory, "broken"));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanProvideTreeBuilder_WhenTheRulesetFailedValidation_DoesNotBuildItAgain()
    {
        // Arrange
        GivenAnInvalidRulesetBeforeTheMqttOne();
        var brokenRulesetId = new RulesetIdentifier(DataPortTreeBuilderRegistry.DataPortCategory, "broken");
        _registry.Initialize();
        _rulesetProvider.ClearReceivedCalls();

        // Act
        _registry.CanProvideTreeBuilder(brokenRulesetId);

        // Assert
        _rulesetProvider.DidNotReceive().GetRuleset(brokenRulesetId);
    }

    [Fact]
    public void CanProvideTreeBuilder_WhenTheRulesetWasBuilt_ReturnsTrue()
    {
        // Arrange
        GivenAnInvalidRulesetBeforeTheMqttOne();
        _registry.Initialize();

        // Act
        var result = _registry.CanProvideTreeBuilder(new RulesetIdentifier(DataPortTreeBuilderRegistry.DataPortCategory, "mqtt"));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanProvideTreeBuilder_WhenTheRulesetAppearedAfterInitialize_BuildsIt()
    {
        // Arrange
        _rulesetProvider.GetRulesetIdentifiers(DataPortTreeBuilderRegistry.DataPortCategory).Returns([]);
        _registry.Initialize();

        // Act
        var result = _registry.CanProvideTreeBuilder(new RulesetIdentifier(DataPortTreeBuilderRegistry.DataPortCategory, "mqtt"));

        // Assert
        result.Should().BeTrue();
        _registry.TryGetTreeBuilderForDataPort(Resources.TestResources.MqttRuleset.Root!.Id, out _).Should().BeTrue();
    }

    [Fact]
    public void CanProvideTreeBuilder_WhenARulesetAppearedAfterInitializeAndFailsValidation_ReturnsFalseAndReportsIt()
    {
        // Arrange
        _rulesetProvider.GetRulesetIdentifiers(DataPortTreeBuilderRegistry.DataPortCategory).Returns([]);
        var brokenRulesetId = new RulesetIdentifier(DataPortTreeBuilderRegistry.DataPortCategory, "broken");
        _rulesetProvider.GetRuleset(brokenRulesetId).Returns(new Ruleset());
        _registry.Initialize();

        // Act
        var result = _registry.CanProvideTreeBuilder(brokenRulesetId);

        // Assert
        result.Should().BeFalse();
        _logger.LatestRecord.Message.Should().Match("Skipping ruleset broken: ?*");
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
