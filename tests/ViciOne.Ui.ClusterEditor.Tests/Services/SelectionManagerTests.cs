using System;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Blazor.Diagrams.Core.Models;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Services;

public class SelectionManagerTests
{
    [Fact]
    public async Task DiagramSelectionChanged_WhenSubscriberThrows_IsolatesAndLogs()
    {
        await using var ctx = new BunitContext();
        var logger = new FakeLogger<SelectionManager>();
        ctx.Services.AddSingleton<ILogger<SelectionManager>>(logger);
        ctx.SetupSelectionManager();
        ctx.CreateDiagramInstance();

        var diagramService = ctx.Services.GetRequiredService<DiagramService>();
        var sut = ctx.Services.GetRequiredService<SelectionManager>();
        sut.AttachDiagramEvents();

        var second = false;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.DiagramSelectionChanged += _ => throw new InvalidOperationException("x");
        sut.DiagramSelectionChanged += _ => { second = true; completion.TrySetResult(); return Task.CompletedTask; };

        var node = new NodeModel();
        diagramService.Diagram.Nodes.Add(node);
        diagramService.Diagram.SelectModel(node, true);

        await completion.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        second.Should().BeTrue();
        logger.Collector.GetSnapshot().Count(l => l.Level == LogLevel.Error).Should().Be(1);

        sut.Dispose();
    }
}
