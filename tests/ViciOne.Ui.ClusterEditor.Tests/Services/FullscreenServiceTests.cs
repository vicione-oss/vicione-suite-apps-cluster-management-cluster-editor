using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.JSInterop;
using NSubstitute;
using ViciOne.Ui.ClusterEditor.Services;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Services;

public class FullscreenServiceTests
{
    [Fact]
    public async Task SetFullscreenAsync_AwaitsAllSubscribers()
    {
        var logger = new FakeLogger<FullscreenService>();
        var js = Substitute.For<IJSRuntime>();
        using var sut = new FullscreenService(js, logger);
        var order = new List<string>();
        sut.FullscreenStateChanged += async _ => { await Task.Delay(20); order.Add("a"); };
        sut.FullscreenStateChanged += _ => { order.Add("b"); return Task.CompletedTask; };

        await sut.SetFullscreen(true);

        sut.IsFullscreen.Should().BeTrue();
        order.Should().BeEquivalentTo(["a", "b"]);
    }

    [Fact]
    public async Task SetFullscreenAsync_WhenSubscriberThrows_IsolatesAndLogs_AndStillSetsState()
    {
        var logger = new FakeLogger<FullscreenService>();
        using var sut = new FullscreenService(Substitute.For<IJSRuntime>(), logger);
        var second = false;
        sut.FullscreenStateChanged += _ => throw new InvalidOperationException("x");
        sut.FullscreenStateChanged += _ => { second = true; return Task.CompletedTask; };

        var act = () => sut.SetFullscreen(true);

        await act.Should().NotThrowAsync();
        second.Should().BeTrue();
        logger.Collector.GetSnapshot().Count(l => l.Level == LogLevel.Error).Should().Be(1);
    }
}
