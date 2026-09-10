using System.Threading.Tasks;
using Bunit;
using ViciOne.Ui.ClusterEditor.Components;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

public class SelectBoxTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();

        // Act
        var component = ctx.Render<SelectBox>(parameters => parameters
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
