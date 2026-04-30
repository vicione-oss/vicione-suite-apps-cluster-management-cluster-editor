using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ViciOne.Ui.Blazor.Components.Resizing.Services;
using ViciOne.Ui.ClusterEditor.Components.Scrolling;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.Scrolling;

public sealed class ScrollbarTests
{
    [Fact]
    public void Component_gets_rendered()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDevExpressBlazor();
        ctx.Services.AddScoped(_ => Substitute.For<IResizeObserver>());

        // Act
        var component = ctx.RenderComponent<Scrollbar>();

        // Assert
        Assert.NotNull(component);
    }
}
