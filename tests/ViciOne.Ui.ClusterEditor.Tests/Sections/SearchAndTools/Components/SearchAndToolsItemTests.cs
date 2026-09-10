using System.Threading.Tasks;
using Bunit;
using ViciOne.Ui.ClusterEditor.Sections.SearchAndTools.Components;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.SearchAndTools.Components;

public class SearchAndToolsItemTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();

        // Act
        var component = ctx.Render<SearchAndToolsItem>();

        // Assert
        Assert.NotNull(component);
    }
}
