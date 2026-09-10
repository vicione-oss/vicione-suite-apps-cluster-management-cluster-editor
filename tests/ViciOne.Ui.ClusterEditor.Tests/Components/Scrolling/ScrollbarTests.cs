using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ViciOne.Ui.Blazor.Components.Resizing.Services;
using ViciOne.Ui.ClusterEditor.Components.Scrolling;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.Scrolling;

public sealed class ScrollbarTests
{
    [Fact]
    public async Task Component_gets_rendered()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.Services.AddScoped(_ => Substitute.For<IResizeObserver>());

        // Act
        var component = ctx.Render<Scrollbar>();

        // Assert
        Assert.NotNull(component);
    }
}
