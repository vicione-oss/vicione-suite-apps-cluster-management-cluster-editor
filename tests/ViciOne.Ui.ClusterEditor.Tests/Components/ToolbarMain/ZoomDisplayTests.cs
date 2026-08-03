using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ViciOne.Ui.Blazor.Components.Resizing.Services;
using ViciOne.Ui.Blazor.Components.Toolbar.Components;
using ViciOne.Ui.ClusterEditor.Components.ToolbarMain;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.ToolbarMain;

public class ZoomDisplayTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddScoped<DiagramEventService>();
        ctx.SetupDiagramService();

        ctx.Services.AddScoped(_ => Substitute.For<IResizeObserver>());
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        // Act
        var toolbar = ctx.RenderComponent<Toolbar>(b => b
            .AddChildContent<ZoomDisplay>());

        // Assert
        toolbar.Should().NotBeNull();
        toolbar.HasComponent<ZoomDisplay>().Should().BeTrue();
    }
}
