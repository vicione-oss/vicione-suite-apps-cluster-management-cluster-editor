using ViciOne.Ui.ClusterEditor.Components;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

public class SelectBoxTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var component = ctx.RenderComponent<SelectBox>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Height, 100)
            .Add(p => p.Left, 10)
            .Add(p => p.Top, 10)
            .Add(p => p.Width, 100)
        );

        // Assert
        Assert.NotNull(component);
    }
}
