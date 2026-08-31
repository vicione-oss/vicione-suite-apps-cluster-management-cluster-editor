using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using ViciOne.Ui.ClusterEditor.Components.NodeEditorServices;
using Xunit;
using TestContext = Bunit.TestContext;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.NodeEditorServices;

public sealed class NodeEditorJsInteropTests
{
    [Fact]
    public async Task FocusDiagramCanvasAsync_InvokesFocusByClass_WithDiagramCanvasArgument()
    {
        // Arrange
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var sut = new NodeEditorJsInterop(ctx.Services.GetRequiredService<IJSRuntime>());

        // Act
        await sut.FocusDiagramCanvasAsync();

        // Assert
        var invocation = ctx.JSInterop.VerifyInvoke("ViciOne.Element.focusByClass");
        invocation.Arguments.Should().ContainSingle().Which.Should().Be("diagram-canvas");
    }

    [Fact]
    public async Task WaitForNodesAsync_InvokesWaitForNodes_WithIdsAndReturnsResult()
    {
        // Arrange
        using var ctx = new TestContext();
        ctx.JSInterop.Setup<bool>("ViciOne.NodeMove.waitForNodes", _ => true).SetResult(true);
        var sut = new NodeEditorJsInterop(ctx.Services.GetRequiredService<IJSRuntime>());
        var ids = new[] { "node-a", "node-b" };

        // Act
        var result = await sut.WaitForNodesAsync(ids, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var invocation = ctx.JSInterop.VerifyInvoke("ViciOne.NodeMove.waitForNodes");
        invocation.Arguments.Should().ContainSingle().Which.Should().BeSameAs(ids);
    }
}
