using ViciOne.Ui.ClusterEditor.Components;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

public class SearchAndFilterComponentTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var component = ctx.RenderComponent<SearchAndFilterComponent>();

        // Assert
        Assert.NotNull(component);
    }
}
