using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using NSubstitute;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Extensions;

[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Suppression of CA2012 is needed for NSubstitute")]
[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup for ValueTask call in unit test")]
public class IJSRuntimeExtensionsTests
{
    [Fact]
    public async Task MeasureNameFieldHeightAsync_ReturnsDefault_OnJsDisconnectedException()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        var name = "Any";
        var token = CancellationToken.None;

        jsRuntime
            .InvokeAsync<int>(
                "ViciOne.Diagram.BlockNode.measureNameFieldHeight",
                Arg.Any<CancellationToken>(),
                Arg.Any<object?[]?>()
            )
            .Returns(_ => new ValueTask<int>(Task.FromException<int>(new JSDisconnectedException("Simulated disconnect"))));

        var expectedDefault = 2 * DiagramSettings.DefaultGridSize;

        // Act
        var actual = await IJSRuntimeExtensions.MeasureNameFieldHeightAsync(jsRuntime, name, token);

        // Assert
        Assert.Equal(expectedDefault, actual);
    }

    [Fact]
    public async Task MeasureNameFieldHeightAsync_ReturnsDefault_OnObjectDisposedException()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        var name = "Any";
        var token = CancellationToken.None;

        jsRuntime
            .InvokeAsync<int>(
                "ViciOne.Diagram.BlockNode.measureNameFieldHeight",
                Arg.Any<CancellationToken>(),
                Arg.Any<object?[]?>()
            )
            .Returns(_ => new ValueTask<int>(Task.FromException<int>(new ObjectDisposedException("Simulated disposed object"))));

        var expectedDefault = 2 * DiagramSettings.DefaultGridSize;

        // Act
        var actual = await IJSRuntimeExtensions.MeasureNameFieldHeightAsync(jsRuntime, name, token);

        // Assert
        Assert.Equal(expectedDefault, actual);
    }

    [Fact]
    public async Task MeasureNameFieldHeightAsync_ReturnsJsValue_WhenInvocationSucceeds()
    {
        // Arrange
        var jsRuntime = Substitute.For<IJSRuntime>();
        using var cts = new CancellationTokenSource();
        var name = "Example Node";
        var expectedHeight = 123;

        jsRuntime
            .InvokeAsync<int>(
                "ViciOne.Diagram.BlockNode.measureNameFieldHeight",
                Arg.Any<CancellationToken>(),
                Arg.Any<object?[]?>()
            )
            .Returns(_ => ValueTask.FromResult(expectedHeight));

        // Act
        var actual = await IJSRuntimeExtensions.MeasureNameFieldHeightAsync(jsRuntime, name, cts.Token);

        // Assert
        Assert.Equal(expectedHeight, actual);

        await jsRuntime.Received(1).InvokeAsync<int>(
            "ViciOne.Diagram.BlockNode.measureNameFieldHeight",
            cts.Token,
            Arg.Is<object?[]?>(args =>
                args != null &&
                args.Length == 3 &&
                Equals(args[0], name) &&
                Equals(args[1], BlockNodeLayout.Width) &&
                Equals(args[2], DiagramSettings.DefaultGridSize)
            )
        );
    }
}
