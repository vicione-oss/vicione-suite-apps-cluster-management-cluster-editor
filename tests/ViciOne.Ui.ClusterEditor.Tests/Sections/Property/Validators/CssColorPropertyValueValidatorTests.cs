using AwesomeAssertions;
using ViciOne.Ui.ClusterEditor.Sections.Property.Validators;
using CssColorMessages = ViciOne.Ui.ClusterEditor.Sections.Property.Validators.Localization.CssColorPropertyValueValidator;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.Property.Validators;

public sealed class CssColorPropertyValueValidatorTests
{
    private readonly CssColorPropertyValueValidator _sut = new();

    [Theory]
    [InlineData(null)]
    [InlineData("#FF0000")]
    [InlineData("rgb(1, 2, 3)")]
    [InlineData("red")]
    public void Validate_ValidValue_ReturnsNull(string? value)
        => _sut.Validate(value).Should().BeNull();

    [Theory]
    [InlineData("")]
    [InlineData("#GGG")]
    [InlineData("red; display:none")]
    public void Validate_InvalidValue_ReturnsMessage(string value)
        => _sut.Validate(value).Should().Be(CssColorMessages.InvalidCssColor);
}
