using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Ui.ClusterEditor.Services;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Services;

public class ClusterBuilderEventBufferTests
{
    [Fact]
    public async Task BufferedEvents_AreFiredOnTheSchedulingSynchronizationContext()
    {
        // The buffered events are projected onto diagram and UI models. Firing them off the
        // Blazor renderer's synchronization context races with the input handled on it and
        // corrupts component state (see Minimap).
        var builder = Substitute.For<IClusterBuilder>();
        using var sut = new ClusterBuilderEventBuffer();
        sut.SetBuilder(builder);

        var fired = new TaskCompletionSource<SynchronizationContext?>(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.LabelPropertiesChanged += _ => fired.TrySetResult(SynchronizationContext.Current);

        var context = new TrackingSynchronizationContext();
        RunWithSynchronizationContext(
            context,
            () => builder.Editors.Label.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(new object(), new PropertyChangedEventArgs("Name")));

        var contextOfFiredEvent = await fired.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        contextOfFiredEvent.Should().BeSameAs(context);
    }

    private static void RunWithSynchronizationContext(SynchronizationContext context, Action action)
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);

        try
        {
            action();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    /// <summary>Runs posted continuations on the thread pool, but keeps itself as the current context.</summary>
    private sealed class TrackingSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
            => ThreadPool.QueueUserWorkItem(_ => Send(d, state));

        public override void Send(SendOrPostCallback d, object? state)
        {
            var previous = Current;
            SetSynchronizationContext(this);

            try
            {
                d(state);
            }
            finally
            {
                SetSynchronizationContext(previous);
            }
        }
    }
}
