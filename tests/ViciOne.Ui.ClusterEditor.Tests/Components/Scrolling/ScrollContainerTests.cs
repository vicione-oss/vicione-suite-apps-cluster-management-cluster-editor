using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ViciOne.Ui.Blazor.Components.Resizing.Services;
using ViciOne.Ui.ClusterEditor.Components.Scrolling;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.Scrolling;

public sealed class ScrollContainerTests
{
    [Fact]
    public void Component_gets_rendered()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddScoped(_ => Substitute.For<IResizeObserver>());

        // Act
        var component = ctx.RenderComponent<ScrollContainer>();

        // Assert
        Assert.NotNull(component);
    }
}
