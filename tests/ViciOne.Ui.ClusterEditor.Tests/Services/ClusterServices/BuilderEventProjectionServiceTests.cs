using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using AwesomeAssertions;
using Blazor.Diagrams.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using NSubstitute;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.Shared.Dx.Services;
using Xunit;
using FunctionBlock = ViciOne.Cluster.Model.FunctionBlock;
using Link = ViciOne.Cluster.Model.Link;
using PropertyChange = (object? Sender, System.ComponentModel.PropertyChangedEventArgs EventArgs);

namespace ViciOne.Ui.ClusterEditor.Tests.Services.ClusterServices;

public sealed class BuilderEventProjectionServiceTests : IDisposable
{
    private readonly ClusterBuilderEventBuffer _buffer = new();
    private readonly List<(ChildContainer Container, string Property)> _containerChanges = [];
    private readonly List<BlockNodeLink> _createdLinks = [];
    private readonly List<BlockNodeLink> _linkRemovals = [];
    private readonly List<string> _propertyChanges = [];
    private readonly DatastoreState _state = new(NullLogger<DatastoreState>.Instance);
    private readonly BuilderEventProjectionService _sut;

    public BuilderEventProjectionServiceTests()
    {
        var diagramProjectionService = new DiagramProjectionService(
            new ComparerService([], NullLogger<ComparerService>.Instance),
            _state,
            Substitute.For<IJSRuntime>(),
            NullLogger<DiagramProjectionService>.Instance);

        _sut = new BuilderEventProjectionService(
            _buffer,
            _state,
            diagramProjectionService,
            NullLogger<BuilderEventProjectionService>.Instance);
        _sut.Attach();

        _state.PropertyChanged += _propertyChanges.Add;
        _state.ContainerPropertyChanged += (container, property) => _containerChanges.Add((container, property));
        _state.ConnectorLinkRemoved += _linkRemovals.Add;
    }

    [Fact]
    public void Attach_SubscribesToEvent_SoBufferedEventsAreProjected()
    {
        // Act - the constructor already called Attach(); raising an event should reach the handler.
        RaiseEnginesAssigned();

        // Assert
        _propertyChanges.Should().ContainSingle().Which.Should().Be(nameof(FunctionBlock.Engine));
    }

    [Fact]
    public void Detach_AfterAttach_StopsProjectingAllBufferedEvents()
    {
        // Arrange - confirm projection is active while attached, then map models the detached handlers
        // would otherwise mutate so we can prove no notifications fire after Detach().
        RaiseEnginesAssigned();
        _propertyChanges.Should().ContainSingle("the service is attached");
        _propertyChanges.Clear();

        var container = MapContainer();
        var link = MapLink(visible: true);

        // Act
        _sut.Detach();
        RaiseEnginesAssigned();
        Raise(nameof(ClusterBuilderEventBuffer.ContainerPropertiesChanged), PropertyChanges((container, nameof(ChildContainer.Name))));
        Raise<IEnumerable<Link>>(nameof(ClusterBuilderEventBuffer.ConnectorLinksRemoved), [link]);

        // Assert
        _propertyChanges.Should().BeEmpty();
        _containerChanges.Should().BeEmpty();
        _linkRemovals.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_AfterAttach_StopsProjectingBufferedEvents()
    {
        // Arrange - confirm projection is active while attached.
        RaiseEnginesAssigned();
        _propertyChanges.Should().ContainSingle("the service is attached");
        _propertyChanges.Clear();

        // Act
        _sut.Dispose();
        RaiseEnginesAssigned();

        // Assert
        _propertyChanges.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        _sut.Dispose();

        var act = _sut.Dispose;

        act.Should().NotThrow();
    }

    public void Dispose()
    {
        _sut.Dispose();

        foreach (var link in _createdLinks)
            link.Dispose();

        _buffer.Dispose();
    }

    private ChildContainer MapContainer()
    {
        var container = new ChildContainer();
        _state.DataflowDiagramMapping.Add(container, new ChildContainerNode());
        return container;
    }

    private FunctionBlock MapFunctionBlock()
    {
        var functionBlock = TestData.GetFunctionBlock();
        _state.DataflowDiagramMapping.Add(functionBlock, new FunctionBlockNode());
        return functionBlock;
    }

    private Label MapLabel()
    {
        var label = TestData.GetLabel();
        _state.DataflowDiagramMapping.Add(label, new LabelNode(new(0, 0)));
        return label;
    }

    private BlockNodeLink MapLink(Link link)
    {
        var node = new LabelNode(new(0, 0));
        var linkNode = new BlockNodeLink(new PortModel(node, PortAlignment.Top), new PortModel(node, PortAlignment.Bottom));
        _createdLinks.Add(linkNode);
        _state.DataflowDiagramMapping.Add(link, linkNode);
        return linkNode;
    }

    private Link MapLink(bool visible)
    {
        var link = new Link { Visible = visible };
        MapLink(link);
        return link;
    }

    [Fact]
    public void OnConnectorLinksAdded_WithVisibleAndHiddenUnmappedLinks_DoesNotProjectOrThrow()
    {
        // Arrange
        IEnumerable<Link> links = [new Link { Visible = true }, new Link { Visible = false }];

        // Act
        var act = () => Raise(nameof(ClusterBuilderEventBuffer.ConnectorLinksAdded), links);

        // Assert
        act.Should().NotThrow();
        _linkRemovals.Should().BeEmpty();
        _propertyChanges.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OnConnectorLinksRemoved_WhenLinkIsMapped_RaisesConnectorLinkRemovedWithItsNode(bool visible)
    {
        // Arrange - a link with no source/destination connectors isolates the removal-notification path.
        var link = new Link { Visible = visible };
        var linkNode = MapLink(link);

        // Act
        Raise<IEnumerable<Link>>(nameof(ClusterBuilderEventBuffer.ConnectorLinksRemoved), [link]);

        // Assert
        _linkRemovals.Should().ContainSingle().Which.Should().BeSameAs(linkNode);
    }

    [Fact]
    public void OnConnectorLinksRemoved_WhenLinkIsNotMapped_DoesNotRaiseOrThrow()
    {
        // Arrange
        IEnumerable<Link> links = [new Link { Visible = false }];

        // Act
        var act = () => Raise(nameof(ClusterBuilderEventBuffer.ConnectorLinksRemoved), links);

        // Assert
        act.Should().NotThrow();
        _linkRemovals.Should().BeEmpty();
    }

    [Fact]
    public void OnConnectorPropertiesChanged_WithNullPropertyNonConnectorOrUnmappedConnector_DoesNotProject()
    {
        // Arrange - covers every guard clause: null property name, a non-IConnector sender, and an
        // unmapped connector (e.g. the FB "setting name" connectors that have no diagram model).
        var changes = PropertyChanges(
            (Substitute.For<IConnector>(), null),
            (new object(), nameof(Connector.EventEnabled)),
            (Substitute.For<IConnector>(), nameof(Connector.EventEnabled)));

        // Act
        var act = () => Raise(nameof(ClusterBuilderEventBuffer.ConnectorPropertiesChanged), changes);

        // Assert
        act.Should().NotThrow();
        _propertyChanges.Should().BeEmpty();
    }

    [Fact]
    public void OnContainerPropertiesChanged_SkipsInvalidEntries_AndStillProcessesMappedContainer()
    {
        // Arrange - skipped entries (null property, wrong sender type, unmapped container) precede a valid
        // mapped container to prove the loop continues past skipped items.
        var mapped = MapContainer();
        var changes = PropertyChanges(
            (mapped, null),
            (new object(), nameof(ChildContainer.Name)),
            (new ChildContainer(), nameof(ChildContainer.Name)),
            (mapped, nameof(ChildContainer.BackColor)));

        // Act
        Raise(nameof(ClusterBuilderEventBuffer.ContainerPropertiesChanged), changes);

        // Assert
        _propertyChanges.Should().ContainSingle().Which.Should().Be(nameof(ChildContainer.BackColor));
        _containerChanges.Should().ContainSingle()
            .Which.Should().Be((mapped, nameof(ChildContainer.BackColor)));
    }

    [Fact]
    public void OnContainerPropertiesChanged_WhenMapped_RaisesPropertyAndContainerNotifications()
    {
        // Arrange
        var container = MapContainer();

        // Act
        Raise(nameof(ClusterBuilderEventBuffer.ContainerPropertiesChanged), PropertyChanges((container, nameof(ChildContainer.Name))));

        // Assert
        _propertyChanges.Should().ContainSingle().Which.Should().Be(nameof(ChildContainer.Name));
        _containerChanges.Should().ContainSingle().Which.Should().Be((container, nameof(ChildContainer.Name)));
    }

    [Theory]
    [InlineData(nameof(ClusterBuilderEventBuffer.EnginesAssigned))]
    [InlineData(nameof(ClusterBuilderEventBuffer.EnginesUnassigned))]
    public void OnFunctionBlockEnginesChanged_WithEmptyList_AlwaysRaisesEngineChangeOnce(string eventName)
    {
        // Act - both the assigned and unassigned events route to the same handler.
        Raise<IEnumerable<FunctionBlock>>(eventName, []);

        // Assert
        _propertyChanges.Should().ContainSingle().Which.Should().Be(nameof(FunctionBlock.Engine));
    }

    [Fact]
    public void OnFunctionBlockEnginesChanged_WithUnmappedFunctionBlocks_RaisesEngineOnceWithoutThrowing()
    {
        // Arrange - unmapped function blocks with no parent container exercise the "no projection" routing
        // while still raising the single Engine notification.
        IEnumerable<FunctionBlock> functionBlocks = [TestData.GetFunctionBlock(), TestData.GetFunctionBlock()];

        // Act
        var act = () => Raise(nameof(ClusterBuilderEventBuffer.EnginesAssigned), functionBlocks);

        // Assert
        act.Should().NotThrow();
        _propertyChanges.Should().ContainSingle().Which.Should().Be(nameof(FunctionBlock.Engine));
    }

    [Fact]
    public void OnFunctionBlockPropertiesChanged_SkipsEngineEvenWhenMapped_AndOtherInvalidEntries()
    {
        // Arrange - the Engine property is owned by the dedicated engine events, so it must be ignored
        // here even for a mapped block; otherwise the mapper would touch state.Builder and throw.
        var mapped = MapFunctionBlock();
        var changes = PropertyChanges(
            (mapped, nameof(FunctionBlock.Engine)),
            (mapped, null),
            (new object(), nameof(FunctionBlock.Name)),
            (TestData.GetFunctionBlock(), nameof(FunctionBlock.Name)));

        // Act
        var act = () => Raise(nameof(ClusterBuilderEventBuffer.FunctionBlockPropertiesChanged), changes);

        // Assert
        act.Should().NotThrow();
        _propertyChanges.Should().BeEmpty();
    }

    [Fact]
    public void OnFunctionBlockPropertiesChanged_WhenMappedAndNonEngineProperty_RaisesPropertyChanged()
    {
        // Arrange
        var functionBlock = MapFunctionBlock();

        // Act
        Raise(nameof(ClusterBuilderEventBuffer.FunctionBlockPropertiesChanged), PropertyChanges((functionBlock, nameof(FunctionBlock.Name))));

        // Assert
        _propertyChanges.Should().ContainSingle().Which.Should().Be(nameof(FunctionBlock.Name));
    }

    [Fact]
    public void OnLabelPropertiesChanged_WhenMapped_RaisesPropertyChanged()
    {
        // Arrange
        var label = MapLabel();

        // Act
        Raise(nameof(ClusterBuilderEventBuffer.LabelPropertiesChanged), PropertyChanges((label, nameof(Label.Content))));

        // Assert
        _propertyChanges.Should().ContainSingle().Which.Should().Be(nameof(Label.Content));
    }

    [Fact]
    public void OnLabelPropertiesChanged_WithNullPropertyNonLabelSenderOrUnmapped_DoesNotProject()
    {
        // Arrange
        var mapped = MapLabel();
        var changes = PropertyChanges(
            (mapped, null),
            (new object(), nameof(Label.Content)),
            (TestData.GetLabel(), nameof(Label.Content)));

        // Act
        var act = () => Raise(nameof(ClusterBuilderEventBuffer.LabelPropertiesChanged), changes);

        // Assert
        act.Should().NotThrow();
        _propertyChanges.Should().BeEmpty();
    }

    private static List<PropertyChange> PropertyChanges(params (object? Sender, string? Property)[] changes)
    {
        var result = new List<PropertyChange>(changes.Length);
        foreach (var (sender, property) in changes)
            result.Add((sender, new PropertyChangedEventArgs(property)));
        return result;
    }

    private void Raise<T>(string eventName, T payload)
    {
        var field = typeof(ClusterBuilderEventBuffer).GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"No backing field found for event '{eventName}'.");

        if (field.GetValue(_buffer) is Action<T> handler)
            handler(payload);
    }

    private void RaiseEnginesAssigned()
        => Raise<IEnumerable<FunctionBlock>>(nameof(ClusterBuilderEventBuffer.EnginesAssigned), []);
}
