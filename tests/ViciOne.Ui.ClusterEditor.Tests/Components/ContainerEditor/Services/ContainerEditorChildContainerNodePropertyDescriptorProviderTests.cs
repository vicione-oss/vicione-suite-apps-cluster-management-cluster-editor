using System.Linq;
using AwesomeAssertions;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Models;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Services;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Sections.Property.Validators;
using ViciOne.Ui.Localization.Resources;
using Xunit;
using LocalCommonVocabulary = ViciOne.Ui.ClusterEditor.Localization.Resources.CommonVocabulary;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.ContainerEditor.Services;

public sealed class ContainerEditorChildContainerNodePropertyDescriptorProviderTests
{
    private readonly ChildContainer _container = new()
    {
        BackColor = "#FFF",
        Description = "InitialDesc",
        ForeColor = "#000",
        Name = "InitialName"
    };

    private readonly IContainerEditor _editor = Substitute.For<IContainerEditor>();

    // ──────────────────────────────────────────────────────────────────────────────
    // BackColor descriptor
    // ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BackColor_descriptor_CanBeSetToNull_is_true()
    {
        // Arrange & Act
        var descriptor = (PropertyDescriptor<ContainerEditorChildContainer, string?>)
            CreateSut().GetPropertyDescriptors(new object()).ToList()[2];

        // Assert
        descriptor.CanBeSetToNull.Should().BeTrue();
    }

    [Fact]
    public void BackColor_descriptor_GetValue_GetDefaultValue_SetValue_ResetValue_behave_correctly()
    {
        // Arrange
        _editor.When(e => e.SetBackColor(Arg.Any<ChildContainer>(), Arg.Any<string?>()))
               .Do(ci => ((ChildContainer)ci[0]).BackColor = (string?)ci[1]);
        var instance = CreateChildContainer();
        var descriptor = (PropertyDescriptor<ContainerEditorChildContainer, string?>)
            CreateSut().GetPropertyDescriptors(new object()).ToList()[2];

        // Act
        var initialValue = descriptor.GetValue(instance);
        var defaultValue = descriptor.GetDefaultValue!(instance);
        descriptor.SetValue!(instance, "#123");
        descriptor.ResetValue!(instance);

        // Assert
        initialValue.Should().Be("#FFF");
        defaultValue.Should().Be(BlockNodeColors.BackgroundDefault);
        _editor.Received(1).SetBackColor(_container, "#123");
        _editor.Received(1).SetBackColor(_container, "#FFF");
    }

    [Fact]
    public void BackColor_descriptor_SetValue_ignores_unsafe_css_value()
    {
        // Arrange
        var instance = CreateChildContainer();
        var descriptor = (PropertyDescriptor<ContainerEditorChildContainer, string?>)
            CreateSut().GetPropertyDescriptors(new object()).ToList()[2];

        // Act
        descriptor.SetValue!(instance, "red; display:none");

        // Assert
        descriptor.GetValue(instance).Should().Be("#FFF");
        _editor.DidNotReceiveWithAnyArgs().SetBackColor(default!, default);
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // Test object creation
    // ──────────────────────────────────────────────────────────────────────────────

    private ContainerEditorChildContainer CreateChildContainer() => new(_editor)
    {
        Backup = new ContainerEditorChildContainerBackup
        {
            BackColor = _container.BackColor,
            Container = _container,
            Description = _container.Description,
            ForeColor = _container.ForeColor,
            Name = _container.Name
        }
    };

    private static ContainerEditorChildContainerNodePropertyDescriptorProvider<object> CreateSut()
        => new();

    // ──────────────────────────────────────────────────────────────────────────────
    // Description descriptor
    // ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Description_descriptor_CanBeSetToNull_is_true()
    {
        // Arrange & Act
        var descriptor = (PropertyDescriptor<ContainerEditorChildContainer, string?>)
            CreateSut().GetPropertyDescriptors(new object()).ToList()[1];

        // Assert
        descriptor.CanBeSetToNull.Should().BeTrue();
    }

    [Fact]
    public void Description_descriptor_GetValue_GetDefaultValue_SetValue_ResetValue_behave_correctly()
    {
        // Arrange
        _editor.When(e => e.SetDescription(Arg.Any<ChildContainer>(), Arg.Any<string?>()))
               .Do(ci => ((ChildContainer)ci[0]).Description = (string?)ci[1]);
        var instance = CreateChildContainer();
        var descriptor = (PropertyDescriptor<ContainerEditorChildContainer, string?>)
            CreateSut().GetPropertyDescriptors(new object()).ToList()[1];

        // Act
        var initialValue = descriptor.GetValue(instance);
        var defaultValue = descriptor.GetDefaultValue!(instance);
        descriptor.SetValue!(instance, "ChangedDesc");
        descriptor.ResetValue!(instance);

        // Assert
        initialValue.Should().Be("InitialDesc");
        defaultValue.Should().BeNull();
        _editor.Received(1).SetDescription(_container, "ChangedDesc");
        _editor.Received(1).SetDescription(_container, "InitialDesc");
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // ForeColor descriptor
    // ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ForeColor_descriptor_CanBeSetToNull_is_true()
    {
        // Arrange & Act
        var descriptor = (PropertyDescriptor<ContainerEditorChildContainer, string?>)
            CreateSut().GetPropertyDescriptors(new object()).ToList()[3];

        // Assert
        descriptor.CanBeSetToNull.Should().BeTrue();
    }

    [Fact]
    public void ForeColor_descriptor_GetValue_GetDefaultValue_SetValue_ResetValue_behave_correctly()
    {
        // Arrange
        _editor.When(e => e.SetForeColor(Arg.Any<ChildContainer>(), Arg.Any<string?>()))
               .Do(ci => ((ChildContainer)ci[0]).ForeColor = (string?)ci[1]);
        var instance = CreateChildContainer();
        var descriptor = (PropertyDescriptor<ContainerEditorChildContainer, string?>)
            CreateSut().GetPropertyDescriptors(new object()).ToList()[3];

        // Act
        var initialValue = descriptor.GetValue(instance);
        var defaultValue = descriptor.GetDefaultValue!(instance);
        descriptor.SetValue!(instance, "#ABC");
        descriptor.ResetValue!(instance);

        // Assert
        initialValue.Should().Be("#000");
        defaultValue.Should().Be(BlockNodeColors.ForegroundDefault);
        _editor.Received(1).SetForeColor(_container, "#ABC");
        _editor.Received(1).SetForeColor(_container, "#000");
    }

    [Fact]
    public void ForeColor_descriptor_SetValue_ignores_unsafe_css_value()
    {
        // Arrange
        var instance = CreateChildContainer();
        var descriptor = (PropertyDescriptor<ContainerEditorChildContainer, string?>)
            CreateSut().GetPropertyDescriptors(new object()).ToList()[3];

        // Act
        descriptor.SetValue!(instance, "red} body{display:none");

        // Assert
        descriptor.GetValue(instance).Should().Be("#000");
        _editor.DidNotReceiveWithAnyArgs().SetForeColor(default!, default);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Color_descriptors_have_css_color_validator(int index)
    {
        // Arrange & Act
        var descriptor = (PropertyDescriptor<ContainerEditorChildContainer, string?>)
            CreateSut().GetPropertyDescriptors(new object()).ToList()[index];

        // Assert
        descriptor.ValueValidators.Should().ContainSingle(v => v is CssColorPropertyValueValidator);
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // Name descriptor
    // ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Name_descriptor_GetValue_returns_current_name()
    {
        // Arrange
        var instance = CreateChildContainer();
        var descriptor = (PropertyDescriptor<ContainerEditorChildContainer, string>)
            CreateSut().GetPropertyDescriptors(new object()).ToList()[0];

        // Act & Assert
        descriptor.GetValue(instance).Should().Be("InitialName");
    }

    [Fact]
    public void Name_descriptor_SetValue_updates_name()
    {
        // Arrange
        _editor.When(e => e.SetName(Arg.Any<ChildContainer>(), Arg.Any<string>()))
               .Do(ci => ((ChildContainer)ci[0]).Name = (string)ci[1]);
        var instance = CreateChildContainer();
        var descriptor = (PropertyDescriptor<ContainerEditorChildContainer, string>)
            CreateSut().GetPropertyDescriptors(new object()).ToList()[0];

        // Act
        descriptor.SetValue!(instance, "NewName");

        // Assert
        _editor.Received(1).SetName(_container, "NewName");
        descriptor.GetValue(instance).Should().Be("NewName");
    }

    [Theory]
    [InlineData(0, nameof(ChildContainer.Name))]
    [InlineData(1, nameof(ChildContainer.Description))]
    [InlineData(2, nameof(ChildContainer.BackColor))]
    [InlineData(3, nameof(ChildContainer.ForeColor))]
    public void Structure_Descriptor_at_index_has_expected_Name(int index, string expectedName)
    {
        // Arrange & Act
        var descriptor = CreateSut().GetPropertyDescriptors(new object())
                                    .Cast<IPropertyDescriptor>()
                                    .ToList()[index];

        // Assert
        descriptor.Name.Should().Be(expectedName);
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // Structure
    // ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Structure_GetPropertyDescriptors_returns_exactly_four_descriptors()
    {
        // Arrange & Act
        var descriptors = CreateSut().GetPropertyDescriptors(new object()).ToList();

        // Assert
        descriptors.Should().HaveCount(4);
    }

    [Fact]
    public void Structure_Name_descriptor_belongs_to_Data_category_and_appearance_descriptors_belong_to_Appearance_category()
    {
        // Arrange & Act
        var descriptors = CreateSut().GetPropertyDescriptors(new object())
                                     .Cast<IPropertyDescriptor>()
                                     .ToList();

        // Assert
        descriptors[0].Category.Should().Be(LocalCommonVocabulary.Data);
        descriptors[1].Category.Should().Be(CommonVocabulary.Appearance);
        descriptors[2].Category.Should().Be(CommonVocabulary.Appearance);
        descriptors[3].Category.Should().Be(CommonVocabulary.Appearance);
    }
}
