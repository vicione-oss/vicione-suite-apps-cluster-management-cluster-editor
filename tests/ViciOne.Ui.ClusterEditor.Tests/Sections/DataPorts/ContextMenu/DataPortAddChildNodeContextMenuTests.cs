using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.ContextMenu;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.ContextMenu;

public class DataPortAddChildNodeContextMenuTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddDataPortAddChildNodeContextMenu();
        ctx.Services.TryAddScoped<DiagramEventService>();

        // Act
        var component = ctx.RenderComponent<DataPortAddChildNodeContextMenu>();

        // Assert
        Assert.NotNull(component);
    }
}
