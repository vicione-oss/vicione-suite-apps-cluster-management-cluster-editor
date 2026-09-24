using AwesomeAssertions;
using Blazor.Diagrams.Core.Geometry;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Models.DiagramModels;

public sealed class LabelNodeTests
{
    private readonly LabelNode _node = new(new Point(0, 0));

    [Fact]
    public void BackgroundColor_ValidColor_IsKept()
    {
        // Act
        _node.BackgroundColor = "#ff0000";

        // Assert
        _node.BackgroundColor.Should().Be("#ff0000");
    }

    [Theory]
    [InlineData("red; background-image: url(https://example.com/t.png)")]
    [InlineData("")]
    public void BackgroundColor_InvalidColor_FallsBackToDefault(string value)
    {
        // Act
        _node.BackgroundColor = value;

        // Assert
        _node.BackgroundColor.Should().Be(LabelColors.BackgroundDefault);
    }

    [Fact]
    public void BorderColor_ValidColor_IsKept()
    {
        // Act
        _node.BorderColor = "rgb(1, 2, 3)";

        // Assert
        _node.BorderColor.Should().Be("rgb(1, 2, 3)");
    }

    [Theory]
    [InlineData("red; display:none")]
    [InlineData("")]
    public void BorderColor_InvalidColor_FallsBackToDefault(string value)
    {
        // Act
        _node.BorderColor = value;

        // Assert
        _node.BorderColor.Should().Be(LabelColors.BorderDefault);
    }
}
