using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Blazor.Diagrams;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.TestHelpers;
using ViciOne.Ui.Shared.Dx.Services;
using Xunit;
using FunctionBlock = ViciOne.Cluster.Model.FunctionBlock;
using Link = ViciOne.Cluster.Model.Link;
using TestContext = Bunit.TestContext;

namespace ViciOne.Ui.ClusterEditor.Tests.Services.ClusterServices;

public sealed class DiagramProjectionServiceTests : IAsyncLifetime
{
    // Distinct, ascending heights so per-index height application and node ordering can be asserted.
    private static readonly int[] s_measuredHeights = [10, 20, 30, 40, 50, 60, 70, 80, 90, 100];
    private readonly IClusterBuilder _builder;
    private readonly TestContext _ctx;
    private readonly IDatastore _datastore;
    private readonly DiagramService _diagramService;
    private readonly Guid _fbDesignId;
    private readonly List<BlockNodeLink> _linksToDispose = [];
    private readonly DatastoreState _state;
    private readonly DiagramProjectionService _sut;

    private static CancellationToken Ct => Xunit.TestContext.Current.CancellationToken;

    public DiagramProjectionServiceTests()
    {
        _ctx = new TestContext();
        _ctx.JSInterop
            .Setup<int[]>("ViciOne.Diagram.BlockNode.measureNameFieldHeights", _ => true)
            .SetResult(s_measuredHeights);

        _ctx.Services.AddSingleton(new ComparerService([], Substitute.For<ILogger<ComparerService>>()));
        _ctx.Services.AddScoped<DiagramEventService>();
        _ctx.Services.AddScoped<ClusterBuilderEventBuffer>();
        _ctx.Services.AddDatastore();
        _ctx.Services.AddScoped<DiagramService>();

        _datastore = _ctx.Services.GetRequiredService<IDatastore>();
        _diagramService = _ctx.Services.GetRequiredService<DiagramService>();
        _diagramService.Diagram = new BlazorDiagram();
        _state = _ctx.Services.GetRequiredService<DatastoreState>();
        _sut = _ctx.Services.GetRequiredService<DiagramProjectionService>();

        _builder = BuilderFactory.Create();
        _fbDesignId = BuilderFactory.FbDesignId;
    }

    private ChildContainer AddUnmappedChildContainer(int x, int y, params IContainerChild[] children)
        => _builder.Editors.Container.AddContainer(_state.ActiveContainer, children: children, location: new(x, y));

    private FunctionBlock AddUnmappedFunctionBlock(int x, int y)
        => _builder.Editors.Container.AddFunctionBlock(_state.ActiveContainer, _fbDesignId, location: new(x, y));

    private Label AddUnmappedLabel(string content, int zIndex)
        => _builder.Editors.Container.AddLabel(_state.ActiveContainer, new(3, 4), new(20, 10), zIndex, content);

    public async ValueTask DisposeAsync()
    {
        foreach (var link in _linksToDispose)
            link.Dispose();

        _builder.Dispose();
        _ctx.Dispose();
        await _datastore.DisposeAsync();
        _diagramService.Dispose();
    }

    public ValueTask InitializeAsync()
        => new(_datastore.Load(_builder, _diagramService, CancellationToken.None));

    // --- AddChildContainersToMapping --------------------------------------------------------------

    [Fact]
    public async Task AddChildContainersToMapping_WhenContainersProvided_CreatesOrderedNodesWithMeasuredHeights_AndRegistersMapping()
    {
        // Arrange
        var first = AddUnmappedChildContainer(10, 10);
        var second = AddUnmappedChildContainer(20, 20);

        // Act
        var result = await _sut.AddChildContainersToMapping([first, second], _diagramService, Ct);

        // Assert
        result.Should().HaveCount(2);
        result.Select(n => n.Name).Should().Equal(first.Name, second.Name);
        result[0].NameFieldHeight.Should().Be(s_measuredHeights[0]);
        result[1].NameFieldHeight.Should().Be(s_measuredHeights[1]);

        _state.DataflowDiagramMapping.TryGetDiagramModel(first, out var firstNode).Should().BeTrue();
        _state.DataflowDiagramMapping.TryGetDiagramModel(second, out var secondNode).Should().BeTrue();
        firstNode.Should().BeSameAs(result[0]);
        secondNode.Should().BeSameAs(result[1]);
    }

    [Fact]
    public async Task AddChildContainersToMapping_WhenListEmpty_ReturnsEmpty_AndDoesNotMutateMapping()
    {
        // Act
        var result = await _sut.AddChildContainersToMapping([], _diagramService, Ct);

        // Assert
        result.Should().BeEmpty();
        _state.DataflowDiagramMapping.GetNodes().Should().BeEmpty();
    }

    // --- AddFunctionBlocksToMapping ---------------------------------------------------------------

    [Fact]
    public async Task AddFunctionBlocksToMapping_WhenFunctionBlocksProvided_CreatesOrderedNodesWithMeasuredHeights_AndRegistersMapping()
    {
        // Arrange
        var first = AddUnmappedFunctionBlock(0, 0);
        var second = AddUnmappedFunctionBlock(200, 0);

        // Act
        var result = await _sut.AddFunctionBlocksToMapping([first, second], _diagramService, Ct);

        // Assert
        result.Should().HaveCount(2);
        result.Select(n => n.Name).Should().Equal(first.Name, second.Name);
        result[0].NameFieldHeight.Should().Be(s_measuredHeights[0]);
        result[1].NameFieldHeight.Should().Be(s_measuredHeights[1]);

        _state.DataflowDiagramMapping.TryGetDiagramModel(first, out var firstNode).Should().BeTrue();
        _state.DataflowDiagramMapping.TryGetDiagramModel(second, out var secondNode).Should().BeTrue();
        firstNode.Should().BeSameAs(result[0]);
        secondNode.Should().BeSameAs(result[1]);
    }

    [Fact]
    public async Task AddFunctionBlocksToMapping_WhenListEmpty_ReturnsEmpty_AndDoesNotMutateMapping()
    {
        // Act
        var result = await _sut.AddFunctionBlocksToMapping([], _diagramService, Ct);

        // Assert
        result.Should().BeEmpty();
        _state.DataflowDiagramMapping.GetNodes().Should().BeEmpty();
    }

    // --- AddLabelsToMapping -----------------------------------------------------------------------

    [Fact]
    public void AddLabelsToMapping_WhenLabelsProvided_CreatesNodesWithCopiedProperties_AndRegistersMapping()
    {
        // Arrange
        var first = AddUnmappedLabel("Label A", zIndex: 3);
        var second = AddUnmappedLabel("Label B", zIndex: 7);

        // Act
        var result = _sut.AddLabelsToMapping([first, second]);

        // Assert
        result.Should().HaveCount(2);
        result[0].Text.Should().Be("Label A");
        result[0].Order.Should().Be(3);
        result[1].Text.Should().Be("Label B");
        result[1].Order.Should().Be(7);

        _state.DataflowDiagramMapping.TryGetDiagramModel(first, out var firstNode).Should().BeTrue();
        _state.DataflowDiagramMapping.TryGetDiagramModel(second, out var secondNode).Should().BeTrue();
        firstNode.Should().BeSameAs(result[0]);
        secondNode.Should().BeSameAs(result[1]);
    }

    [Fact]
    public void AddLabelsToMapping_WhenListEmpty_ReturnsEmpty()
    {
        // Act
        var result = _sut.AddLabelsToMapping([]);

        // Assert
        result.Should().BeEmpty();
        _state.DataflowDiagramMapping.GetLabelModels().Should().BeEmpty();
    }

    // --- AddLinksToMapping ------------------------------------------------------------------------

    [Fact]
    public async Task AddLinksToMapping_WhenLinksProvided_CreatesNodes_AndRegistersMapping()
    {
        // Arrange - links require their endpoint connectors to already be mapped.
        var source = AddUnmappedFunctionBlock(0, 0);
        var target = AddUnmappedFunctionBlock(200, 0);
        await _sut.AddFunctionBlocksToMapping([source, target], _diagramService, Ct);
        var link = _builder.Editors.Connector.AddLink(source.SystemOutputs.First(), target.SystemInputs.First());

        // Act
        var result = _sut.AddLinksToMapping([link]);
        _linksToDispose.AddRange(result);

        // Assert
        result.Should().ContainSingle();
        _state.DataflowDiagramMapping.TryGetDiagramModel(link, out var linkNode).Should().BeTrue();
        linkNode.Should().BeSameAs(result[0]);
    }

    [Fact]
    public void AddLinksToMapping_WhenLinkMissingConnectors_ThrowsInvalidOperationException()
    {
        // Arrange - a link without a source connector cannot be projected.
        var act = () => _sut.AddLinksToMapping([new Link()]);

        // Act & Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*source connector*");
    }

    [Fact]
    public void AddLinksToMapping_WhenListEmpty_ReturnsEmpty()
    {
        // Act
        var result = _sut.AddLinksToMapping([]);

        // Assert
        result.Should().BeEmpty();
        _state.DataflowDiagramMapping.GetNodeLinks().Should().BeEmpty();
    }

    // --- GetActiveContainerContainerLinks ---------------------------------------------------------

    [Fact]
    public void GetActiveContainerContainerLinks_WhenChildContainerHasInputConnectorOnly_ReturnsEmpty()
    {
        // Arrange - the boundary-crossing link enters the container, producing an input connector,
        // which the output-only enumeration must ignore.
        var outside = AddUnmappedFunctionBlock(0, 0);
        var inside = AddUnmappedFunctionBlock(200, 0);
        _builder.Editors.Connector.AddLink(outside.SystemOutputs.First(), inside.SystemInputs.First());
        AddUnmappedChildContainer(100, 100, inside);

        // Act
        var links = _sut.GetActiveContainerContainerLinks();

        // Assert
        links.Should().BeEmpty();
    }

    [Fact]
    public void GetActiveContainerContainerLinks_WhenChildContainerHasVisibleOutputLink_YieldsLink()
    {
        // Arrange - the source moves into the container, so the crossing link leaves it through an
        // output connector and stays visible in the active container.
        var inside = AddUnmappedFunctionBlock(0, 0);
        var outside = AddUnmappedFunctionBlock(200, 0);
        _builder.Editors.Connector.AddLink(inside.SystemOutputs.First(), outside.SystemInputs.First());
        AddUnmappedChildContainer(100, 100, inside);

        // Act
        var links = _sut.GetActiveContainerContainerLinks().ToList();

        // Assert
        links.Should().ContainSingle();
        links[0].Visible.Should().BeTrue();
    }

    [Fact]
    public void GetActiveContainerContainerLinks_WhenNoChildContainers_ReturnsEmpty()
    {
        // Act
        var links = _sut.GetActiveContainerContainerLinks();

        // Assert
        links.Should().BeEmpty();
    }

    // --- GetActiveContainerFunctionBlockLinks -----------------------------------------------------

    [Fact]
    public void GetActiveContainerFunctionBlockLinks_WhenFunctionBlocksLinked_YieldsVisibleLink()
    {
        // Arrange
        var source = AddUnmappedFunctionBlock(0, 0);
        var target = AddUnmappedFunctionBlock(200, 0);
        var link = _builder.Editors.Connector.AddLink(source.SystemOutputs.First(), target.SystemInputs.First());

        // Act
        var links = _sut.GetActiveContainerFunctionBlockLinks();

        // Assert
        links.Should().ContainSingle().Which.Should().BeSameAs(link);
    }

    [Fact]
    public void GetActiveContainerFunctionBlockLinks_WhenNoLinks_ReturnsEmpty()
    {
        // Arrange - a function block with no links exercises the loop without yielding.
        AddUnmappedFunctionBlock(0, 0);

        // Act
        var links = _sut.GetActiveContainerFunctionBlockLinks();

        // Assert
        links.Should().BeEmpty();
    }
}
