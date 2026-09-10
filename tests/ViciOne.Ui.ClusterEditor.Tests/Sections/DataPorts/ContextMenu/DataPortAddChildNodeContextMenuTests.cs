using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.ContextMenu;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.ContextMenu;

public class DataPortAddChildNodeContextMenuTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.Services.AddDataPortAddChildNodeContextMenu();
        ctx.Services.TryAddScoped<DiagramEventService>();

        // Act
        var component = ctx.Render<DataPortAddChildNodeContextMenu>();

        // Assert
        Assert.NotNull(component);
    }
}
