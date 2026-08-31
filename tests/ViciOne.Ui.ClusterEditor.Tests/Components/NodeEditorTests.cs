using AwesomeAssertions;
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
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupNodeEditor();

        // Act
        var component = ctx.RenderComponent<NodeEditor>();

        // Assert
        Assert.NotNull(component);
    }

    [Fact]
    public void EdgeDraggingPointerUp_AfterDispose_RoutesThroughGuard_AndNeverThrows()
    {
        using var ctx = new Bunit.TestContext();
        ctx.SetupNodeEditor();

        var diagramEvents = ctx.Services.GetRequiredService<DiagramEventService>();
        var cut = ctx.RenderComponent<NodeEditor>();

        cut.Instance.Dispose();

        var act = () => diagramEvents.InvokeEdgeDraggingPointerUp(new PointerEventArgs());

        act.Should().NotThrow();
    }

    [Fact]
    public void EdgeDraggingPointerMove_AfterDispose_RoutesThroughGuard_AndNeverThrows()
    {
        using var ctx = new Bunit.TestContext();
        ctx.SetupNodeEditor();

        var diagramEvents = ctx.Services.GetRequiredService<DiagramEventService>();
        var cut = ctx.RenderComponent<NodeEditor>();

        cut.Instance.Dispose();

        var act = () => diagramEvents.InvokeEdgeDraggingPointerMove(new PointerEventArgs());

        act.Should().NotThrow();
    }
}
