using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using ViciOne.Ui.ClusterEditor.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Extensions;

public class FuncExtensionsTests
{
    [Fact]
    [Trait(Traits.Category, Traits.Manual)]
    public async Task InvokeEventAsync_WithOneArg_AllHandlersExecuteInParallel()
    {
        // Arrange
        var totalTime = double.MinValue;
        var delayMs = 100;
        var invokationCount = 0;
        Func<int, Task> eventHandler = async arg =>
        {
            invokationCount++;
            await Task.Delay(delayMs);
        };
        eventHandler += async arg =>
        {
            invokationCount++;
            await Task.Delay(delayMs);
        };
        eventHandler += async arg =>
        {
            invokationCount++;
            await Task.Delay(delayMs);
        };

        // Act
        await Task.Run(async () =>
        {
            var startTime = DateTime.UtcNow;
            await eventHandler.InvokeEventAsync(5);
            var endTime = DateTime.UtcNow;
            totalTime = (endTime - startTime).TotalMilliseconds;
        }, TestContext.Current.CancellationToken);

        // Assert - if executed in parallel, total time should be <300ms
        Assert.True(totalTime < delayMs * 3, $"Expected parallel execution to take less than {delayMs * 3}ms, but took {totalTime}ms");
        Assert.Equal(3, invokationCount);
    }

    [Fact]
    public async Task InvokeEventAsync_WithOneArg_WithEmptyEventName_ShouldUseUnknownInLog()
    {
        // Arrange
        var logger = new FakeLogger<FuncExtensionsTests>();
        var exception = new InvalidOperationException("Test exception");
        Func<int, Task> eventHandler = arg => throw exception;

        // Act
        await eventHandler.InvokeEventAsync(0, logger, "   ");

        // Assert
        var numberValidLogRecords = logger
            .Collector
            .GetSnapshot()
            .Count(log => log.Level == LogLevel.Error &&
                                     log.Exception == exception &&
                                     log.Message.Contains("Unknown", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, numberValidLogRecords);
    }

    [Fact]
    public async Task InvokeEventAsync_WithOneArg_WithException_ShouldContinueInvokingOtherHandlers()
    {
        // Arrange
        var firstHandlerInvoked = false;
        var thirdHandlerInvoked = false;
        Func<string, Task> eventHandler = arg =>
        {
            firstHandlerInvoked = true;
            return Task.CompletedTask;
        };
        eventHandler += arg => throw new InvalidOperationException("Test exception");
        eventHandler += arg =>
        {
            thirdHandlerInvoked = true;
            return Task.CompletedTask;
        };

        // Act
        await eventHandler.InvokeEventAsync("test");

        // Assert
        Assert.True(firstHandlerInvoked);
        Assert.True(thirdHandlerInvoked);
    }

    [Fact]
    public async Task InvokeEventAsync_WithOneArg_WithException_ShouldLogError()
    {
        // Arrange
        var logger = new FakeLogger<FuncExtensionsTests>();
        var exception = new InvalidOperationException("Test exception");
        Func<string, Task> eventHandler = arg => throw exception;

        // Act
        await eventHandler.InvokeEventAsync("test", logger, "TestEvent");

        // Assert
        var numberValidLogRecords = logger
            .Collector
            .GetSnapshot()
            .Count(log => log.Level == LogLevel.Error &&
                                     log.Exception == exception);
        Assert.Equal(1, numberValidLogRecords);
    }

    [Fact]
    public async Task InvokeEventAsync_WithOneArg_WithExceptionAndNoLogger_ShouldNotThrow()
    {
        // Arrange
        Func<string, Task> eventHandler = arg => throw new InvalidOperationException("Test exception");

        // Act & Assert
        await eventHandler.InvokeEventAsync("test");
    }

    [Fact]
    public async Task InvokeEventAsync_WithOneArg_WithMultipleHandlers_ShouldInvokeAllHandlersWithCorrectArgument()
    {
        // Arrange
        var argValue = 5;
        var expectedSumOfArgs = argValue * 3;
        var sumOfArgs = 0;
        Func<int, Task> eventHandler = arg =>
        {
            sumOfArgs += arg;
            return Task.CompletedTask;
        };
        eventHandler += arg =>
        {
            sumOfArgs += arg;
            return Task.CompletedTask;
        };
        eventHandler += arg =>
        {
            sumOfArgs += arg;
            return Task.CompletedTask;
        };

        // Act
        await eventHandler.InvokeEventAsync(argValue);

        // Assert
        Assert.Equal(expectedSumOfArgs, sumOfArgs);
    }

    [Fact]
    public async Task InvokeEventAsync_WithOneArg_WithNullEventHandler_ShouldCompleteSuccessfully()
    {
        // Arrange
        Func<string, Task>? eventHandler = null;

        // Act & Assert
        await eventHandler.InvokeEventAsync("test");
    }

    [Fact]
    public async Task InvokeEventAsync_WithOneArg_WithNullEventName_ShouldUseUnknownInLog()
    {
        // Arrange
        var logger = new FakeLogger<FuncExtensionsTests>();
        var exception = new InvalidOperationException("Test exception");
        Func<int, Task> eventHandler = arg => throw exception;

        // Act
        await eventHandler.InvokeEventAsync(0, logger, null);

        // Assert
        var numberValidLogRecords = logger
            .Collector
            .GetSnapshot()
            .Count(log => log.Level == LogLevel.Error &&
                                     log.Exception == exception &&
                                     log.Message.Contains("Unknown", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, numberValidLogRecords);
    }

    [Fact]
    [Trait(Traits.Category, Traits.Manual)]
    public async Task InvokeEventAsync_WithoutArg_AllHandlersExecuteInParallel()
    {
        // Arrange
        var totalTime = double.MinValue;
        var delayMs = 100;
        var invokationCount = 0;
        Func<Task> eventHandler = async () =>
        {
            invokationCount++;
            await Task.Delay(delayMs);
        };
        eventHandler += async () =>
        {
            invokationCount++;
            await Task.Delay(delayMs);
        };
        eventHandler += async () =>
        {
            invokationCount++;
            await Task.Delay(delayMs);
        };

        // Act
        await Task.Run(async () =>
        {
            var startTime = DateTime.UtcNow;
            await eventHandler.InvokeEventAsync();
            var endTime = DateTime.UtcNow;
            totalTime = (endTime - startTime).TotalMilliseconds;
        }, TestContext.Current.CancellationToken);

        // Assert - if executed in parallel, total time should be <300ms
        Assert.True(totalTime < delayMs * 3, $"Expected parallel execution to take less than {delayMs * 3}ms, but took {totalTime}ms");
        Assert.Equal(3, invokationCount);
    }

    [Fact]
    public async Task InvokeEventAsync_WithoutArg_WithEmptyEventName_ShouldUseUnknownInLog()
    {
        // Arrange
        var logger = new FakeLogger<FuncExtensionsTests>();
        var exception = new InvalidOperationException("Test exception");
        Func<Task> eventHandler = () => throw exception;

        // Act
        await eventHandler.InvokeEventAsync(logger, "   ");

        // Assert
        var numberValidLogRecords = logger
            .Collector
            .GetSnapshot()
            .Count(log => log.Level == LogLevel.Error &&
                                     log.Exception == exception &&
                                     log.Message.Contains("Unknown", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, numberValidLogRecords);
    }

    [Fact]
    public async Task InvokeEventAsync_WithoutArg_WithException_ShouldContinueInvokingOtherHandlers()
    {
        // Arrange
        var firstHandlerInvoked = false;
        var thirdHandlerInvoked = false;
        Func<Task> eventHandler = () =>
        {
            firstHandlerInvoked = true;
            return Task.CompletedTask;
        };
        eventHandler += () => throw new InvalidOperationException("Test exception");
        eventHandler += () =>
        {
            thirdHandlerInvoked = true;
            return Task.CompletedTask;
        };

        // Act
        await eventHandler.InvokeEventAsync();

        // Assert
        Assert.True(firstHandlerInvoked);
        Assert.True(thirdHandlerInvoked);
    }

    [Fact]
    public async Task InvokeEventAsync_WithoutArg_WithException_ShouldLogError()
    {
        // Arrange
        var logger = new FakeLogger<FuncExtensionsTests>();
        var exception = new InvalidOperationException("Test exception");
        Func<Task> eventHandler = () => throw exception;

        // Act
        await eventHandler.InvokeEventAsync(logger, "TestEvent");

        // Assert
        var numberValidLogRecords = logger
            .Collector
            .GetSnapshot()
            .Count(log => log.Level == LogLevel.Error &&
                                     log.Exception == exception);
        Assert.Equal(1, numberValidLogRecords);
    }

    [Fact]
    public async Task InvokeEventAsync_WithoutArg_WithExceptionAndNoLogger_ShouldNotThrow()
    {
        // Arrange
        Func<Task> eventHandler = () => throw new InvalidOperationException("Test exception");

        // Act & Assert
        await eventHandler.InvokeEventAsync();
    }

    [Fact]
    public async Task InvokeEventAsync_WithoutArg_WithMultipleHandlers_ShouldInvokeAllHandlers()
    {
        // Arrange
        var invokationCount = 0;
        Func<Task> eventHandler = () =>
        {
            invokationCount++;
            return Task.CompletedTask;
        };
        eventHandler += () =>
        {
            invokationCount++;
            return Task.CompletedTask;
        };
        eventHandler += () =>
        {
            invokationCount++;
            return Task.CompletedTask;
        };

        // Act
        await eventHandler.InvokeEventAsync();

        // Assert
        Assert.Equal(3, invokationCount);
    }

    [Fact]
    public async Task InvokeEventAsync_WithoutArg_WithNullEventHandler_ShouldCompleteSuccessfully()
    {
        // Arrange
        Func<Task>? eventHandler = null;

        // Act & Assert
        await eventHandler.InvokeEventAsync();
    }

    [Fact]
    public async Task InvokeEventAsync_WithoutArg_WithNullEventName_ShouldUseUnknownInLog()
    {
        // Arrange
        var logger = new FakeLogger<FuncExtensionsTests>();
        var exception = new InvalidOperationException("Test exception");
        Func<Task> eventHandler = () => throw exception;

        // Act
        await eventHandler.InvokeEventAsync(logger, null);

        // Assert
        var numberValidLogRecords = logger
            .Collector
            .GetSnapshot()
            .Count(log => log.Level == LogLevel.Error &&
                                     log.Exception == exception &&
                                     log.Message.Contains("Unknown", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, numberValidLogRecords);
    }

    [Fact]
    [Trait(Traits.Category, Traits.Manual)]
    public async Task InvokeEventAsync_WithThreeArgs_AllHandlersExecuteInParallel()
    {
        // Arrange
        var totalTime = double.MinValue;
        var delayMs = 100;
        var invokationCount = 0;
        Func<int, int, int, Task> eventHandler = async (arg1, arg2, arg3) =>
        {
            invokationCount++;
            await Task.Delay(delayMs);
        };
        eventHandler += async (arg1, arg2, arg3) =>
        {
            invokationCount++;
            await Task.Delay(delayMs);
        };
        eventHandler += async (arg1, arg2, arg3) =>
        {
            invokationCount++;
            await Task.Delay(delayMs);
        };

        // Act
        await Task.Run(async () =>
        {
            var startTime = DateTime.UtcNow;
            await eventHandler.InvokeEventAsync(5, 2, 3);
            var endTime = DateTime.UtcNow;
            totalTime = (endTime - startTime).TotalMilliseconds;
        }, TestContext.Current.CancellationToken);


        // Assert - if executed in parallel, total time should be <300ms
        Assert.True(totalTime < delayMs * 3, $"Expected parallel execution to take less than {delayMs * 3}ms, but took {totalTime}ms");
        Assert.Equal(3, invokationCount);
    }

    [Fact]
    public async Task InvokeEventAsync_WithThreeArgs_WithEmptyEventName_ShouldUseUnknownInLog()
    {
        // Arrange
        var logger = new FakeLogger<FuncExtensionsTests>();
        var exception = new InvalidOperationException("Test exception");
        Func<int, int, int, Task> eventHandler = (arg1, arg2, arg3) => throw exception;

        // Act
        await eventHandler.InvokeEventAsync(0, 0, 0, logger, "   ");

        // Assert
        var numberValidLogRecords = logger
            .Collector
            .GetSnapshot()
            .Count(log => log.Level == LogLevel.Error &&
                                     log.Exception == exception &&
                                     log.Message.Contains("Unknown", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, numberValidLogRecords);
    }

    [Fact]
    public async Task InvokeEventAsync_WithThreeArgs_WithException_ShouldContinueInvokingOtherHandlers()
    {
        // Arrange
        var firstHandlerInvoked = false;
        var thirdHandlerInvoked = false;
        Func<string, int, int, Task> eventHandler = (arg1, arg2, arg3) =>
        {
            firstHandlerInvoked = true;
            return Task.CompletedTask;
        };
        eventHandler += (arg1, arg2, arg3) => throw new InvalidOperationException("Test exception");
        eventHandler += (arg1, arg2, arg3) =>
        {
            thirdHandlerInvoked = true;
            return Task.CompletedTask;
        };

        // Act
        await eventHandler.InvokeEventAsync("test", 0, 0);

        // Assert
        Assert.True(firstHandlerInvoked);
        Assert.True(thirdHandlerInvoked);
    }

    [Fact]
    public async Task InvokeEventAsync_WithThreeArgs_WithException_ShouldLogError()
    {
        // Arrange
        var logger = new FakeLogger<FuncExtensionsTests>();
        var exception = new InvalidOperationException("Test exception");
        Func<string, int, int, Task> eventHandler = (arg1, arg2, arg3) => throw exception;

        // Act
        await eventHandler.InvokeEventAsync("test", 0, 0, logger, "TestEvent");

        // Assert
        var numberValidLogRecords = logger
            .Collector
            .GetSnapshot()
            .Count(log => log.Level == LogLevel.Error &&
                                     log.Exception == exception);
        Assert.Equal(1, numberValidLogRecords);
    }

    [Fact]
    public async Task InvokeEventAsync_WithThreeArgs_WithExceptionAndNoLogger_ShouldNotThrow()
    {
        // Arrange
        Func<string, int, int, Task> eventHandler = (arg1, arg2, arg3) => throw new InvalidOperationException("Test exception");

        // Act & Assert
        await eventHandler.InvokeEventAsync("test", 0, 0);
    }

    [Fact]
    public async Task InvokeEventAsync_WithThreeArgs_WithMultipleHandlers_ShouldInvokeAllHandlersWithCorrectArguments()
    {
        // Arrange
        var arg1Value = 5;
        var arg2Value = 2;
        var arg3Value = 25;
        var expectedSumOfArgs = (arg1Value + arg2Value + arg3Value) * 3;
        var sumOfArgs = 0;
        Func<int, int, int, Task> eventHandler = (arg1, arg2, arg3) =>
        {
            sumOfArgs += arg1;
            sumOfArgs += arg2;
            sumOfArgs += arg3;
            return Task.CompletedTask;
        };
        eventHandler += (arg1, arg2, arg3) =>
        {
            sumOfArgs += arg1;
            sumOfArgs += arg2;
            sumOfArgs += arg3;
            return Task.CompletedTask;
        };
        eventHandler += (arg1, arg2, arg3) =>
        {
            sumOfArgs += arg1;
            sumOfArgs += arg2;
            sumOfArgs += arg3;
            return Task.CompletedTask;
        };

        // Act
        await eventHandler.InvokeEventAsync(arg1Value, arg2Value, arg3Value);

        // Assert
        Assert.Equal(expectedSumOfArgs, sumOfArgs);
    }

    [Fact]
    public async Task InvokeEventAsync_WithThreeArgs_WithNullEventHandler_ShouldCompleteSuccessfully()
    {
        // Arrange
        Func<string, int, int, Task>? eventHandler = null;

        // Act & Assert
        await eventHandler.InvokeEventAsync("test", 0, 0);
    }

    [Fact]
    public async Task InvokeEventAsync_WithThreeArgs_WithNullEventName_ShouldUseUnknownInLog()
    {
        // Arrange
        var logger = new FakeLogger<FuncExtensionsTests>();
        var exception = new InvalidOperationException("Test exception");
        Func<int, int, int, Task> eventHandler = (arg1, arg2, arg3) => throw exception;

        // Act
        await eventHandler.InvokeEventAsync(0, 0, 0, logger, null);

        // Assert
        var numberValidLogRecords = logger
            .Collector
            .GetSnapshot()
            .Count(log => log.Level == LogLevel.Error &&
                                     log.Exception == exception &&
                                     log.Message.Contains("Unknown", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, numberValidLogRecords);
    }
}
