using ViciOne.Ui.ClusterEditor.Sections.SearchAndTools.Components;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.SearchAndTools.Components;

public class SearchAndToolsItemTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var component = ctx.RenderComponent<SearchAndToolsItem>();

        // Assert
        Assert.NotNull(component);
    }
}
