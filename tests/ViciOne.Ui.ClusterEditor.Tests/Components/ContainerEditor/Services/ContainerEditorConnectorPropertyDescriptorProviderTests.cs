using System.Linq;
using AwesomeAssertions;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Models;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Services;
using ViciOne.Ui.ClusterEditor.Localization.Resources;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.ContainerEditor.Services;

public sealed class ContainerEditorConnectorPropertyDescriptorProviderTests
{
    private readonly ContainerConnectorInput _connectorInput = new()
    {
        Description = "InitialDesc",
        Index = 1,
        Name = "InitialName",
        ShortName = "IN"
    };

    private readonly IConnectorEditor _editor = Substitute.For<IConnectorEditor>();

    private ContainerEditorConnector CreateConnector() => new(_editor)
    {
        Backup = new ContainerEditorConnectorBackup
        {
            Connector = _connectorInput,
            Description = _connectorInput.Description,
            Index = (int)_connectorInput.Index,
            Name = _connectorInput.Name,
            ShortName = _connectorInput.ShortName
        }
    };

    private static ContainerEditorConnectorPropertyDescriptorProvider<object> CreateSut() => new();

    // ──────────────────────────────────────────────────────────────────────────────
    // Description descriptor
    // ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Description_descriptor_GetValue_returns_current_description()
    {
        // Arrange
        var connector = CreateConnector();
        var descriptor = (PropertyDescriptor<ContainerEditorConnector, string?>)
            CreateSut().GetPropertyDescriptors(new object()).First();

        // Act & Assert
        descriptor.GetValue(connector).Should().Be(_connectorInput.Description);
    }

    [Fact]
    public void Description_descriptor_SetValue_and_ResetValue_call_editor()
    {
        // Arrange
        // Configure mock to actually mutate the connector so the setter guard does not short-circuit on reset.
        _editor.When(e => e.SetDescription(Arg.Any<ContainerConnector>(), Arg.Any<string>()))
               .Do(ci => ((ContainerConnector)ci[0]).Description = (string)ci[1]);
        var connector = CreateConnector();
        var descriptor = (PropertyDescriptor<ContainerEditorConnector, string?>)
            CreateSut().GetPropertyDescriptors(new object()).First();

        // Act
        descriptor.SetValue!(connector, "Changed");
        descriptor.ResetValue!(connector);

        // Assert
        _editor.Received(1).SetDescription(_connectorInput, "Changed");
        _editor.Received(1).SetDescription(_connectorInput, "InitialDesc");
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // FunctionBlock descriptor
    // ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void FunctionBlock_descriptor_GetValue_returns_FunctionBlock_name_and_Enabled_is_always_false()
    {
        // Arrange
        var connector = CreateConnector();
        var descriptor = (PropertyDescriptor<ContainerEditorConnector, string>)
            CreateSut().GetPropertyDescriptors(new object()).ToList()[1];

        // Act
        var value = descriptor.GetValue(connector);
        var enabled = descriptor.Enabled!(connector);

        // Assert
        value.Should().Be(_connectorInput.FunctionBlock.Name);
        enabled.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // Name descriptor
    // ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Name_descriptor_GetValue_and_GetDefaultValue_return_current_and_backup_name()
    {
        // Arrange
        var connector = CreateConnector();
        var descriptor = (PropertyDescriptor<ContainerEditorConnector, string>)
            CreateSut().GetPropertyDescriptors(new object()).ToList()[2];

        // Act & Assert
        descriptor.GetValue(connector).Should().Be("InitialName");
        descriptor.GetDefaultValue!(connector).Should().Be("InitialName");
    }

    [Fact]
    public void Name_descriptor_SetValue_and_ResetValue_call_editor()
    {
        // Arrange
        // Configure mock to actually mutate the connector so the setter guard does not short-circuit on reset.
        _editor.When(e => e.SetName(Arg.Any<ContainerConnector>(), Arg.Any<string>()))
               .Do(ci => ((ContainerConnector)ci[0]).Name = (string)ci[1]);
        var connector = CreateConnector();
        var descriptor = (PropertyDescriptor<ContainerEditorConnector, string>)
            CreateSut().GetPropertyDescriptors(new object()).ToList()[2];

        // Act
        descriptor.SetValue!(connector, "NewName");
        descriptor.ResetValue!(connector);

        // Assert
        _editor.Received(1).SetName(_connectorInput, "NewName");
        _editor.Received(1).SetName(_connectorInput, "InitialName");
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // ShortName descriptor
    // ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ShortName_descriptor_GetValue_and_GetDefaultValue_return_current_and_backup_short_name()
    {
        // Arrange
        var connector = CreateConnector();
        var descriptor = (PropertyDescriptor<ContainerEditorConnector, string>)
            CreateSut().GetPropertyDescriptors(new object()).ToList()[3];

        // Act & Assert
        descriptor.GetValue(connector).Should().Be("IN");
        descriptor.GetDefaultValue!(connector).Should().Be("IN");
    }

    [Fact]
    public void ShortName_descriptor_SetValue_and_ResetValue_call_editor_and_do_not_throw_when_BlockNodeConnector_is_null()
    {
        // Arrange
        // Configure mock to actually mutate the connector so the setter guard does not short-circuit on reset.
        _editor.When(e => e.SetShortName(Arg.Any<ContainerConnector>(), Arg.Any<string>()))
               .Do(ci => ((ContainerConnector)ci[0]).ShortName = (string)ci[1]);
        var connector = CreateConnector(); // BlockNodeConnector is null by default
        var descriptor = (PropertyDescriptor<ContainerEditorConnector, string>)
            CreateSut().GetPropertyDescriptors(new object()).ToList()[3];

        // Act
        var setAct = () => descriptor.SetValue!(connector, "OUT");
        var resetAct = () => descriptor.ResetValue!(connector);

        // Assert
        setAct.Should().NotThrow();
        resetAct.Should().NotThrow();
        _editor.Received(1).SetShortName(_connectorInput, "OUT");
        _editor.Received(1).SetShortName(_connectorInput, "IN");
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // Structure
    // ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Structure_All_descriptors_belong_to_the_Data_category()
    {
        // Arrange & Act
        var descriptors = CreateSut().GetPropertyDescriptors(new object())
                                     .Cast<IPropertyDescriptor>()
                                     .ToList();

        // Assert
        descriptors.Should().AllSatisfy(d => d.Category.Should().Be(CommonVocabulary.Data));
    }

    [Theory]
    [InlineData(0, nameof(ContainerEditorConnector.Description))]
    [InlineData(2, nameof(ContainerEditorConnector.Name))]
    [InlineData(3, nameof(ContainerEditorConnector.ShortName))]
    public void Structure_Descriptor_at_index_has_expected_Name(int index, string expectedName)
    {
        // Arrange & Act
        var descriptor = CreateSut().GetPropertyDescriptors(new object())
                                    .Cast<IPropertyDescriptor>()
                                    .ToList()[index];

        // Assert
        descriptor.Name.Should().Be(expectedName);
    }

    [Fact]
    public void Structure_FunctionBlock_descriptor_name_contains_FunctionBlock_and_Name()
    {
        // Arrange & Act
        var descriptor = CreateSut().GetPropertyDescriptors(new object())
                                    .Cast<IPropertyDescriptor>()
                                    .ToList()[1];

        // Assert
        descriptor.Name.Should().Contain(nameof(FunctionBlock))
                       .And.Contain(nameof(FunctionBlock.Name));
    }

    [Fact]
    public void Structure_GetPropertyDescriptors_returns_exactly_four_descriptors()
    {
        // Arrange & Act
        var descriptors = CreateSut().GetPropertyDescriptors(new object()).ToList();

        // Assert
        descriptors.Should().HaveCount(4);
    }
}
