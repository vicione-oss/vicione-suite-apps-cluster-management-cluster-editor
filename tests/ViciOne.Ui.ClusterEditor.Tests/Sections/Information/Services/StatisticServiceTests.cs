using System;
using System.ComponentModel;
using System.Threading.Tasks;
using AwesomeAssertions;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Sections.Information.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.Information.Services;

public class StatisticServiceTests
{
    [Fact]
    public async Task StatisticChanged_FiresOncePerBufferedChange_AfterSeveralBuilderChanges()
    {
        var builder = Substitute.For<IClusterBuilder>();
        using var buffer = new ClusterBuilderEventBuffer();
        buffer.SetBuilder(builder);

        var datastore = Substitute.For<IDatastore>();
        using var sut = new StatisticService(buffer, datastore);

        datastore.BuilderChanged += Raise.Event<Func<Task>>();
        datastore.BuilderChanged += Raise.Event<Func<Task>>();

        var statisticChangedCount = 0;
        sut.StatisticChanged += _ => statisticChangedCount++;

        // Subscribed after the service, so it runs once all of the service's handlers have run.
        var flushed = new TaskCompletionSource();
        buffer.ConnectorPropertiesChanged += _ => flushed.TrySetResult();

        builder.Editors.Connector.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(new object(), new PropertyChangedEventArgs(nameof(Connector.Published)));
        await flushed.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        statisticChangedCount.Should().Be(1);
    }
}
