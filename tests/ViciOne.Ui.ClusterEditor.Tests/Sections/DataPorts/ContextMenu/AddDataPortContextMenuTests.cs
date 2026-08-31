using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.ContextMenu;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.ContextMenu;

public class AddDataPortContextMenuTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddDataPortContextMenu();
        ctx.Services.TryAddScoped<DiagramEventService>();
        ctx.Services.TryAddScoped(_ => Substitute.For<IRulesetProvider>());

        // Act
        var component = ctx.RenderComponent<AddDataPortContextMenu>();

        // Assert
        Assert.NotNull(component);
    }
}
