using System;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using ViciOne.Ui.ClusterEditor.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Extensions;

public class AsyncGuardTests
{
    [Fact]
    public async Task ExecuteSafely_WhenOperationThrows_LogsErrorAndDoesNotThrow()
    {
        var logger = new FakeLogger<AsyncGuardTests>();
        var ex = new InvalidOperationException("boom");

        var act = () => AsyncGuard.ExecuteSafely(() => throw ex, logger, "Caller");

        await act.Should().NotThrowAsync();
        logger.Collector.GetSnapshot()
            .Count(l => l.Level == LogLevel.Error && l.Exception == ex)
            .Should().Be(1);
    }

    [Fact]
    public async Task ExecuteSafely_WhenOperationSucceeds_RunsToCompletion()
    {
        var logger = new FakeLogger<AsyncGuardTests>();
        var ran = false;

        await AsyncGuard.ExecuteSafely(async () => { await Task.Yield(); ran = true; }, logger, "Caller");

        ran.Should().BeTrue();
        logger.Collector.GetSnapshot().Should().BeEmpty();
    }
}
