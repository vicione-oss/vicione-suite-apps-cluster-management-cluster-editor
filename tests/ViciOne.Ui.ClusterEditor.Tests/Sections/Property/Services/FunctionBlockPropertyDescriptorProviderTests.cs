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

public sealed class FunctionBlockPropertyDescriptorProviderTests
{
    private readonly IFunctionBlockEditor _editor = Substitute.For<IFunctionBlockEditor>();
    private readonly FunctionBlock _functionBlock = new();

    [Theory]
    [InlineData("rgb(1, 2, 3)")]
    [InlineData(null)]
    public void BackColor_SetValue_SafeValue_IsForwardedToEditor(string? value)
    {
        // Act
        GetDescriptor(nameof(FunctionBlock.BackColor)).SetValue!(_functionBlock, value);

        // Assert
        _editor.Received(1).SetBackColor(_functionBlock, value);
    }

    [Fact]
    public void BackColor_SetValue_UnsafeValue_IsIgnored()
    {
        // Act
        GetDescriptor(nameof(FunctionBlock.BackColor)).SetValue!(_functionBlock, "red; display:none");

        // Assert
        _editor.DidNotReceiveWithAnyArgs().SetBackColor(default!, default);
    }

    [Theory]
    [InlineData("#000")]
    [InlineData(null)]
    public void ForeColor_SetValue_SafeValue_IsForwardedToEditor(string? value)
    {
        // Act
        GetDescriptor(nameof(FunctionBlock.ForeColor)).SetValue!(_functionBlock, value);

        // Assert
        _editor.Received(1).SetForeColor(_functionBlock, value);
    }

    [Fact]
    public void ForeColor_SetValue_UnsafeValue_IsIgnored()
    {
        // Act
        GetDescriptor(nameof(FunctionBlock.ForeColor)).SetValue!(_functionBlock, "red} body{display:none");

        // Assert
        _editor.DidNotReceiveWithAnyArgs().SetForeColor(default!, default);
    }

    [Theory]
    [InlineData(nameof(FunctionBlock.BackColor))]
    [InlineData(nameof(FunctionBlock.ForeColor))]
    public void ColorDescriptor_HasCssColorValidator(string name)
        => Assert.Single(GetDescriptor(name).ValueValidators!.OfType<CssColorPropertyValueValidator>());

    private PropertyDescriptor<FunctionBlock, string?> GetDescriptor(string name)
    {
        var editors = Substitute.For<IEditors>();
        editors.FunctionBlock.Returns(_editor);
        var builder = Substitute.For<IClusterBuilder>();
        builder.Editors.Returns(editors);
        var datastore = Substitute.For<IDatastore>();
        datastore.Builder.Returns(builder);

        return (PropertyDescriptor<FunctionBlock, string?>)new FunctionBlockPropertyDescriptorProvider<object>(datastore)
            .GetPropertyDescriptors(new object())
            .First(d => d.Name == name);
    }
}
