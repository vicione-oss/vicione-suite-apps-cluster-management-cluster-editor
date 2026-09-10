using System.Threading.Tasks;
using Bunit;
using ViciOne.Ui.ClusterEditor.Components;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

public class SearchAndFilterComponentTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();

        // Act
        var component = ctx.Render<SearchAndFilterComponent>();

        // Assert
        Assert.NotNull(component);
    }
}
