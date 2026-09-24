using System.Linq;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.ClusterEditor.Sections.Property.Services;
using ViciOne.Ui.ClusterEditor.Sections.Property.Validators;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.Property.Services;

public sealed class LabelPropertyDescriptorProviderTests
{
    private readonly ILabelEditor _editor = Substitute.For<ILabelEditor>();
    private readonly Label _label = new();

    [Theory]
    [InlineData("rgb(1, 2, 3)")]
    [InlineData(null)]
    public void BackColor_SetValue_SafeValue_IsForwardedToEditor(string? value)
    {
        // Act
        GetDescriptor(nameof(Label.BackColor)).SetValue!(_label, value);

        // Assert
        _editor.Received(1).SetBackColor(_label, value);
    }

    [Fact]
    public void BackColor_SetValue_UnsafeValue_IsIgnored()
    {
        // Act
        GetDescriptor(nameof(Label.BackColor)).SetValue!(_label, "red; display:none");

        // Assert
        _editor.DidNotReceiveWithAnyArgs().SetBackColor(default!, default);
    }

    [Theory]
    [InlineData("#000")]
    [InlineData(null)]
    public void BorderColor_SetValue_SafeValue_IsForwardedToEditor(string? value)
    {
        // Act
        GetDescriptor(nameof(Label.BorderColor)).SetValue!(_label, value);

        // Assert
        _editor.Received(1).SetBorderColor(_label, value);
    }

    [Fact]
    public void BorderColor_SetValue_UnsafeValue_IsIgnored()
    {
        // Act
        GetDescriptor(nameof(Label.BorderColor)).SetValue!(_label, "red} body{display:none");

        // Assert
        _editor.DidNotReceiveWithAnyArgs().SetBorderColor(default!, default);
    }

    [Theory]
    [InlineData(nameof(Label.BackColor))]
    [InlineData(nameof(Label.BorderColor))]
    public void ColorDescriptor_HasCssColorValidator(string name)
        => Assert.Single(GetDescriptor(name).ValueValidators!.OfType<CssColorPropertyValueValidator>());

    private PropertyDescriptor<Label, string?> GetDescriptor(string name)
    {
        var editors = Substitute.For<IEditors>();
        editors.Label.Returns(_editor);
        var builder = Substitute.For<IClusterBuilder>();
        builder.Editors.Returns(editors);
        var datastore = Substitute.For<IDatastore>();
        datastore.Builder.Returns(builder);

        return (PropertyDescriptor<Label, string?>)new LabelPropertyDescriptorProvider<object>(datastore)
            .GetPropertyDescriptors(new object())
            .First(d => d.Name == name);
    }
}
