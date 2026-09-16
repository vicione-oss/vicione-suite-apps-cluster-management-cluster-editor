using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using NSubstitute;
using ViciOne.Ui.ClusterEditor.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Extensions;

[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Suppression of CA2012 is needed for NSubstitute")]
[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup for ValueTask call in unit test")]
public class IJSObjectReferenceExtensionsTests
{
    private const string Identifier = "ViciOne.Test.identifier";

    private static async Task AssertTryInvokeIsGuarded(Exception exception, string expectedEventName)
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();

        jsObjectReference
            .InvokeAsync<int>(Identifier, Arg.Any<object?[]?>())
            .Returns(_ => new ValueTask<int>(Task.FromException<int>(exception)));

        // Act
        var (success, value) = await jsObjectReference.TryInvoke<int>(logger, Identifier);

        // Assert
        success.Should().BeFalse();
        value.Should().Be(default);

        var record = logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Level.Should().Be(LogLevel.Debug);
        record.Id.Name.Should().Be(expectedEventName);
        record.Exception.Should().BeSameAs(exception);
    }

    private static async Task AssertTryInvokeVoidIsGuarded(Exception exception, string expectedEventName)
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();

        jsObjectReference
            .InvokeAsync<IJSVoidResult>(Identifier, Arg.Any<object?[]?>())
            .Returns(_ => new ValueTask<IJSVoidResult>(Task.FromException<IJSVoidResult>(exception)));

        // Act
        var success = await jsObjectReference.TryInvokeVoid(logger, Identifier);

        // Assert
        success.Should().BeFalse();

        var record = logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Level.Should().Be(LogLevel.Debug);
        record.Id.Name.Should().Be(expectedEventName);
        record.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public async Task TryDisposeAsync_LogsAndReturnsFalse_OnJsDisconnectedException()
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();
        var exception = new JSDisconnectedException("Simulated disconnect");

        jsObjectReference
            .DisposeAsync()
            .Returns(_ => new ValueTask(Task.FromException(exception)));

        // Act
        var success = await jsObjectReference.TryDisposeAsync(logger);

        // Assert
        success.Should().BeFalse();

        var record = logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Level.Should().Be(LogLevel.Debug);
        record.Id.Name.Should().Be(JsInteropGuard.UnavailableEventName);
        record.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public async Task TryDisposeAsync_LogsTheCallingMember_WhenDisposalFails()
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();

        jsObjectReference
            .DisposeAsync()
            .Returns(_ => new ValueTask(Task.FromException(new JSDisconnectedException("Simulated disconnect"))));

        // Act - the identifier is the caller, because disposal has no JavaScript function name.
        await jsObjectReference.TryDisposeAsync(logger);

        // Assert
        var record = logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Message.Should().Contain(nameof(TryDisposeAsync_LogsTheCallingMember_WhenDisposalFails));
    }

    [Fact]
    public async Task TryDisposeAsync_PropagatesJsException_BecauseItSignalsADefect()
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();
        var exception = new JSException("Simulated JavaScript error");

        jsObjectReference
            .DisposeAsync()
            .Returns(_ => new ValueTask(Task.FromException(exception)));

        // Act
        var act = async () => await jsObjectReference.TryDisposeAsync(logger);

        // Assert
        (await act.Should().ThrowAsync<JSException>()).Which.Should().BeSameAs(exception);
        logger.Collector.GetSnapshot().Should().BeEmpty();
    }

    [Fact]
    public async Task TryDisposeAsync_ReturnsTrue_WhenDisposalSucceeds()
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();

        jsObjectReference.DisposeAsync().Returns(ValueTask.CompletedTask);

        // Act
        var success = await jsObjectReference.TryDisposeAsync(logger);

        // Assert
        success.Should().BeTrue();
        logger.Collector.GetSnapshot().Should().BeEmpty();

        await jsObjectReference.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task TryInvoke_DoesNotForwardCallerToken_WhenInvocationSucceeds()
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();
        using var cts = new CancellationTokenSource();

        jsObjectReference
            .InvokeAsync<int>(Identifier, Arg.Any<object?[]?>())
            .Returns(ValueTask.FromResult(7));

        // Act
        var (success, value) = await jsObjectReference.TryInvoke<int>(logger, Identifier, cts.Token, "argument");

        // Assert
        success.Should().BeTrue();
        value.Should().Be(7);
        logger.Collector.GetSnapshot().Should().BeEmpty();

        // The framework cancels a pending call from the token callback, which races the circuit completing the
        // same call and can crash it. Only the wait for the result may observe the caller's token.
        await jsObjectReference.Received(1).InvokeAsync<int>(
            Identifier,
            Arg.Is<object?[]?>(args => args != null && args.Length == 1 && Equals(args[0], "argument")));
        await jsObjectReference.DidNotReceive().InvokeAsync<int>(Identifier, Arg.Any<CancellationToken>(), Arg.Any<object?[]?>());
    }

    [Fact]
    public async Task TryInvoke_PropagatesCancellation_WhenCallerCancelsWhileCallIsPending()
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();
        using var cts = new CancellationTokenSource();
        var pendingCall = new TaskCompletionSource<int>();

        jsObjectReference
            .InvokeAsync<int>(Identifier, Arg.Any<object?[]?>())
            .Returns(_ => new ValueTask<int>(pendingCall.Task));

        var task = jsObjectReference.TryInvoke<int>(logger, Identifier, cts.Token);

        // Act
        await cts.CancelAsync();
        var act = async () => await task;

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        logger.Collector.GetSnapshot().Should().BeEmpty();

        // The abandoned call still completes normally once JavaScript replies.
        pendingCall.TrySetResult(7).Should().BeTrue();
    }

    [Fact]
    public async Task TryInvoke_PropagatesCancellation_WithoutInvoking_WhenCallerTokenIsAlreadySignalled()
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = async () => await jsObjectReference.TryInvoke<int>(logger, Identifier, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        logger.Collector.GetSnapshot().Should().BeEmpty();
        await jsObjectReference.DidNotReceive().InvokeAsync<int>(Identifier, Arg.Any<object?[]?>());
    }

    [Fact]
    public async Task TryInvoke_PropagatesJsException_BecauseItSignalsADefect()
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();
        var exception = new JSException("Simulated JavaScript error");

        jsObjectReference
            .InvokeAsync<int>(Identifier, Arg.Any<object?[]?>())
            .Returns(_ => new ValueTask<int>(Task.FromException<int>(exception)));

        // Act
        var act = async () => await jsObjectReference.TryInvoke<int>(logger, Identifier);

        // Assert - a failure inside JavaScript is a defect, not a vanished target.
        (await act.Should().ThrowAsync<JSException>()).Which.Should().BeSameAs(exception);
        logger.Collector.GetSnapshot().Should().BeEmpty();
    }

    [Fact]
    public Task TryInvoke_ReturnsFailure_OnJsDisconnectedException()
        => AssertTryInvokeIsGuarded(new JSDisconnectedException("Simulated disconnect"), JsInteropGuard.UnavailableEventName);

    [Fact]
    public Task TryInvoke_ReturnsFailure_OnObjectDisposedException()
        => AssertTryInvokeIsGuarded(new ObjectDisposedException("Simulated disposed object"), JsInteropGuard.DisposedEventName);

    [Fact]
    public async Task TryInvoke_ReturnsFailure_OnTaskCanceledException_WhenCallerTokenIsNotSignalled()
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();
        using var cts = new CancellationTokenSource();
        var exception = new TaskCanceledException("Simulated interop timeout");

        jsObjectReference
            .InvokeAsync<int>(Identifier, Arg.Any<object?[]?>())
            .Returns(_ => new ValueTask<int>(Task.FromException<int>(exception)));

        // Act
        var (success, value) = await jsObjectReference.TryInvoke<int>(logger, Identifier, cts.Token);

        // Assert
        success.Should().BeFalse();
        value.Should().Be(default);

        var record = logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Level.Should().Be(LogLevel.Debug);
        record.Id.Name.Should().Be(JsInteropGuard.UnavailableEventName);
        record.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public async Task TryInvoke_ReturnsNullValue_WhenJavaScriptReturnsNull()
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();

        jsObjectReference
            .InvokeAsync<int[]>(Identifier, Arg.Any<object?[]?>())
            .Returns(new ValueTask<int[]>(Task.FromResult<int[]>(null!)));

        // Act
        var (success, value) = await jsObjectReference.TryInvoke<int[]>(logger, Identifier);

        // Assert
        success.Should().BeTrue();
        value.Should().BeNull();
        logger.Collector.GetSnapshot().Should().BeEmpty();
    }

    [Fact]
    public async Task TryInvoke_ReturnsValue_WhenInvocationSucceeds()
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();

        jsObjectReference
            .InvokeAsync<int>(Identifier, Arg.Any<object?[]?>())
            .Returns(ValueTask.FromResult(42));

        // Act
        var (success, value) = await jsObjectReference.TryInvoke<int>(logger, Identifier);

        // Assert
        success.Should().BeTrue();
        value.Should().Be(42);
        logger.Collector.GetSnapshot().Should().BeEmpty();
    }

    [Fact]
    public async Task TryInvoke_UsesArgsOnlyOverload_WhenNoTokenIsGiven()
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();

        jsObjectReference
            .InvokeAsync<int>(Identifier, Arg.Any<object?[]?>())
            .Returns(ValueTask.FromResult(42));

        // Act
        await jsObjectReference.TryInvoke<int>(logger, Identifier);

        // Assert - only the args-only overload lets the framework apply its JS interop timeout.
        await jsObjectReference.Received(1).InvokeAsync<int>(Identifier, Arg.Any<object?[]?>());
        await jsObjectReference.DidNotReceive().InvokeAsync<int>(Identifier, Arg.Any<CancellationToken>(), Arg.Any<object?[]?>());
    }

    [Fact]
    public async Task TryInvokeVoid_DoesNotForwardCallerToken_WhenInvocationSucceeds()
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();
        using var cts = new CancellationTokenSource();

        jsObjectReference
            .InvokeAsync<IJSVoidResult>(Identifier, Arg.Any<object?[]?>())
            .Returns(ValueTask.FromResult<IJSVoidResult>(null!));

        // Act
        var success = await jsObjectReference.TryInvokeVoid(logger, Identifier, cts.Token, "argument");

        // Assert
        success.Should().BeTrue();
        logger.Collector.GetSnapshot().Should().BeEmpty();

        // The framework cancels a pending call from the token callback, which races the circuit completing the
        // same call and can crash it. Only the wait for the result may observe the caller's token.
        await jsObjectReference.Received(1).InvokeAsync<IJSVoidResult>(
            Identifier,
            Arg.Is<object?[]?>(args => args != null && args.Length == 1 && Equals(args[0], "argument")));
        await jsObjectReference.DidNotReceive().InvokeAsync<IJSVoidResult>(Identifier, Arg.Any<CancellationToken>(), Arg.Any<object?[]?>());
    }

    [Fact]
    public async Task TryInvokeVoid_PropagatesCancellation_WhenCallerCancelsWhileCallIsPending()
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();
        using var cts = new CancellationTokenSource();
        var pendingCall = new TaskCompletionSource<IJSVoidResult>();

        jsObjectReference
            .InvokeAsync<IJSVoidResult>(Identifier, Arg.Any<object?[]?>())
            .Returns(_ => new ValueTask<IJSVoidResult>(pendingCall.Task));

        var task = jsObjectReference.TryInvokeVoid(logger, Identifier, cts.Token);

        // Act
        await cts.CancelAsync();
        var act = async () => await task;

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        logger.Collector.GetSnapshot().Should().BeEmpty();

        // The abandoned call still completes normally once JavaScript replies.
        pendingCall.TrySetResult(null!).Should().BeTrue();
    }

    [Fact]
    public async Task TryInvokeVoid_PropagatesCancellation_WithoutInvoking_WhenCallerTokenIsAlreadySignalled()
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = async () => await jsObjectReference.TryInvokeVoid(logger, Identifier, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        logger.Collector.GetSnapshot().Should().BeEmpty();
        await jsObjectReference.DidNotReceive().InvokeAsync<IJSVoidResult>(Identifier, Arg.Any<object?[]?>());
    }

    [Fact]
    public async Task TryInvokeVoid_PropagatesJsException_BecauseItSignalsADefect()
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();
        var exception = new JSException("Simulated JavaScript error");

        jsObjectReference
            .InvokeAsync<IJSVoidResult>(Identifier, Arg.Any<object?[]?>())
            .Returns(_ => new ValueTask<IJSVoidResult>(Task.FromException<IJSVoidResult>(exception)));

        // Act
        var act = async () => await jsObjectReference.TryInvokeVoid(logger, Identifier);

        // Assert - a failure inside JavaScript is a defect, not a vanished target.
        (await act.Should().ThrowAsync<JSException>()).Which.Should().BeSameAs(exception);
        logger.Collector.GetSnapshot().Should().BeEmpty();
    }

    [Fact]
    public Task TryInvokeVoid_ReturnsFalse_OnJsDisconnectedException()
        => AssertTryInvokeVoidIsGuarded(new JSDisconnectedException("Simulated disconnect"), JsInteropGuard.UnavailableEventName);

    [Fact]
    public Task TryInvokeVoid_ReturnsFalse_OnObjectDisposedException()
        => AssertTryInvokeVoidIsGuarded(new ObjectDisposedException("Simulated disposed object"), JsInteropGuard.DisposedEventName);

    [Fact]
    public async Task TryInvokeVoid_ReturnsFalse_OnTaskCanceledException_WhenCallerTokenIsNotSignalled()
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();
        using var cts = new CancellationTokenSource();
        var exception = new TaskCanceledException("Simulated interop timeout");

        jsObjectReference
            .InvokeAsync<IJSVoidResult>(Identifier, Arg.Any<object?[]?>())
            .Returns(_ => new ValueTask<IJSVoidResult>(Task.FromException<IJSVoidResult>(exception)));

        // Act
        var success = await jsObjectReference.TryInvokeVoid(logger, Identifier, cts.Token);

        // Assert
        success.Should().BeFalse();

        var record = logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Level.Should().Be(LogLevel.Debug);
        record.Id.Name.Should().Be(JsInteropGuard.UnavailableEventName);
        record.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public async Task TryInvokeVoid_ReturnsTrue_WhenInvocationSucceeds()
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();

        jsObjectReference
            .InvokeAsync<IJSVoidResult>(Identifier, Arg.Any<object?[]?>())
            .Returns(ValueTask.FromResult<IJSVoidResult>(null!));

        // Act
        var success = await jsObjectReference.TryInvokeVoid(logger, Identifier);

        // Assert
        success.Should().BeTrue();
        logger.Collector.GetSnapshot().Should().BeEmpty();
    }

    [Fact]
    public async Task TryInvokeVoid_UsesArgsOnlyOverload_WhenNoTokenIsGiven()
    {
        // Arrange
        var jsObjectReference = Substitute.For<IJSObjectReference>();
        var logger = new FakeLogger<IJSObjectReferenceExtensionsTests>();

        jsObjectReference
            .InvokeAsync<IJSVoidResult>(Identifier, Arg.Any<object?[]?>())
            .Returns(ValueTask.FromResult<IJSVoidResult>(null!));

        // Act
        await jsObjectReference.TryInvokeVoid(logger, Identifier);

        // Assert - only the args-only overload lets the framework apply its JS interop timeout.
        await jsObjectReference.Received(1).InvokeAsync<IJSVoidResult>(Identifier, Arg.Any<object?[]?>());
        await jsObjectReference.DidNotReceive().InvokeAsync<IJSVoidResult>(Identifier, Arg.Any<CancellationToken>(), Arg.Any<object?[]?>());
    }
}
