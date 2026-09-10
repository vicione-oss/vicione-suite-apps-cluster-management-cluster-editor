using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.ClusterEditor.Components;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

public class NodeEditorTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupNodeEditor();

        // Act
        var component = ctx.Render<NodeEditor>();

        // Assert
        Assert.NotNull(component);
    }

    [Fact]
    public async Task EdgeDraggingPointerUp_AfterDispose_RoutesThroughGuard_AndNeverThrows()
    {
        await using var ctx = new BunitContext();
        ctx.SetupNodeEditor();

        var diagramEvents = ctx.Services.GetRequiredService<DiagramEventService>();
        var cut = ctx.Render<NodeEditor>();

        cut.Instance.Dispose();

        var act = () => diagramEvents.InvokeEdgeDraggingPointerUp(new PointerEventArgs());

        act.Should().NotThrow();
    }

    [Fact]
    public async Task EdgeDraggingPointerMove_AfterDispose_RoutesThroughGuard_AndNeverThrows()
    {
        await using var ctx = new BunitContext();
        ctx.SetupNodeEditor();

        var diagramEvents = ctx.Services.GetRequiredService<DiagramEventService>();
        var cut = ctx.Render<NodeEditor>();

        cut.Instance.Dispose();

        var act = () => diagramEvents.InvokeEdgeDraggingPointerMove(new PointerEventArgs());

        act.Should().NotThrow();
    }
}
