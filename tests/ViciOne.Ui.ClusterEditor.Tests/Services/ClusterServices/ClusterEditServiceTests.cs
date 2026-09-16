using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.TestHelpers;
using ViciOne.Ui.Shared.Dx.Services;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Services.ClusterServices;

public sealed class ClusterEditServiceTests : IAsyncLifetime
{
    private readonly IClusterBuilder _builder;
    private readonly BunitContext _ctx;
    private readonly IDatastore _datastore;
    private readonly DiagramEventService _diagramEvents;
    private readonly DiagramService _diagramService;
    private readonly Guid _fbDesignId;
    private readonly List<BlockNodeLink> _linksToDispose = [];
    private readonly DatastoreState _state;
    private readonly ClusterEditService _sut;

    private static CancellationToken Ct => Xunit.TestContext.Current.CancellationToken;

    public ClusterEditServiceTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop
            .Setup<int[]>("ViciOne.Diagram.BlockNode.measureNameFieldHeights", _ => true)
            .SetResult([.. Enumerable.Repeat(0, 10)]);

        _ctx.Services.AddSingleton(new ComparerService([], Substitute.For<ILogger<ComparerService>>()));
        _ctx.Services.AddScoped<DiagramEventService>();
        _ctx.Services.AddScoped<ClusterBuilderEventBuffer>();
        _ctx.Services.AddDatastore();
        _ctx.Services.AddScoped<DiagramService>();

        _datastore = _ctx.Services.GetRequiredService<IDatastore>();
        _diagramService = _ctx.Services.GetRequiredService<DiagramService>();
        _diagramService.Diagram = new BlazorDiagram();
        _state = _ctx.Services.GetRequiredService<DatastoreState>();
        _diagramEvents = _ctx.Services.GetRequiredService<DiagramEventService>();
        _sut = _ctx.Services.GetRequiredService<ClusterEditService>();

        _builder = BuilderFactory.Create();
        _fbDesignId = BuilderFactory.FbDesignId;
    }

    public ValueTask InitializeAsync()
        => new(_datastore.Load(_builder, _diagramService, CancellationToken.None));

    public async ValueTask DisposeAsync()
    {
        foreach (var link in _linksToDispose)
            link.Dispose();

        _builder.Dispose();
        await _ctx.DisposeAsync();
        await _datastore.DisposeAsync();
        _diagramService.Dispose();
    }

    private async Task<(FunctionBlockNode Node, FunctionBlock Model)> AddFunctionBlockAsync(Point position)
    {
        var node = await _sut.AddFunctionBlock(_diagramService, _fbDesignId, position, Ct);
        return (node, _state.DataflowDiagramMapping.GetModel(node));
    }

    private BlockNodeLink CreateNodeLink(FunctionBlock source, FunctionBlock target)
    {
        var sourcePort = _state.DataflowDiagramMapping.GetDiagramModel(source.SystemOutputs.First());
        var targetPort = _state.DataflowDiagramMapping.GetDiagramModel(target.SystemInputs.First());
        var link = new BlockNodeLink(sourcePort, targetPort);
        _linksToDispose.Add(link);
        return link;
    }

    // --- AddFunctionBlock -------------------------------------------------------------------------

    [Fact]
    public async Task AddFunctionBlock_RegistersNode_AppliesDefaultColors_AndParentsUnderActiveContainer()
    {
        // Act
        var node = await _sut.AddFunctionBlock(_diagramService, _fbDesignId, new Point(10, 20), Ct);

        // Assert
        node.Should().NotBeNull();
        var functionBlock = _state.DataflowDiagramMapping.GetModel(node);
        _state.DataflowDiagramMapping.TryGetDiagramModel(functionBlock, out _).Should().BeTrue();
        functionBlock.Container.Should().Be(_state.ActiveContainer);
        functionBlock.BackColor.Should().Be(BlockNodeColors.BackgroundDefault);
        functionBlock.ForeColor.Should().Be(BlockNodeColors.ForegroundDefault);
        functionBlock.X.Should().Be(10);
        functionBlock.Y.Should().Be(20);
    }

    [Fact]
    public async Task AddFunctionBlock_WhenValidEnginesExist_AssignsFirstValidEngine()
    {
        // Arrange
        _state.ValidDataflowEngines.Should().NotBeEmpty();
        var expectedEngine = _state.ValidDataflowEngines.First();

        // Act
        var node = await _sut.AddFunctionBlock(_diagramService, _fbDesignId, new Point(0, 0), Ct);

        // Assert
        var functionBlock = _state.DataflowDiagramMapping.GetModel(node);
        functionBlock.Engine.Should().NotBeNull();
        functionBlock.Engine.Name.Should().Be(expectedEngine.Name);
    }

    [Fact]
    public async Task AddFunctionBlock_WhenCallerTokenIsCancelled_StillRegistersNode()
    {
        // Arrange - the library ghost drag cancels its token when the pointer leaves the diagram while
        // the function block is being created. The model object must still get a node so the drag leave
        // can remove both again.
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var node = await _sut.AddFunctionBlock(_diagramService, _fbDesignId, new Point(0, 0), cts.Token);

        // Assert
        var functionBlock = _state.DataflowDiagramMapping.GetModel(node);
        _state.DataflowDiagramMapping.TryGetDiagramModel(functionBlock, out _).Should().BeTrue();
        node.NameFieldHeight.Should().Be(2 * DiagramSettings.DefaultGridSize);
    }

    [Fact]
    public async Task AddFunctionBlock_WhenNoValidEngines_DoesNotAssignEngine()
    {
        // Arrange
        _state.SetValidDataflowEngines([]);

        // Act
        var node = await _sut.AddFunctionBlock(_diagramService, _fbDesignId, new Point(0, 0), Ct);

        // Assert
        _state.DataflowDiagramMapping.GetModel(node).Engine.Should().BeNull();
    }

    // --- AddChildContainer ------------------------------------------------------------------------

    [Fact]
    public async Task AddChildContainer_RegistersNode_AppliesDefaultColors_AndRaisesContainerAdded()
    {
        // Arrange
        ChildContainer? raised = null;
        _diagramEvents.ContainerAdded += c => raised = c;

        // Act
        var node = await _sut.AddChildContainer(_diagramService, new Point(30, 40), Ct);

        // Assert
        node.Should().NotBeNull();
        var container = _state.DataflowDiagramMapping.GetModel(node);
        container.Parent.Should().Be(_state.ActiveContainer);
        container.BackColor.Should().Be(BlockNodeColors.BackgroundDefault);
        container.ForeColor.Should().Be(BlockNodeColors.ForegroundDefault);
        raised.Should().BeSameAs(container);
    }

    [Fact]
    public async Task AddChildContainer_WithChildren_ReparentsChildrenIntoNewContainer()
    {
        // Arrange
        var (_, functionBlock) = await AddFunctionBlockAsync(new Point(10, 10));

        // Act
        var node = await _sut.AddChildContainer(_diagramService, new Point(100, 100), Ct, functionBlock);

        // Assert
        var container = _state.DataflowDiagramMapping.GetModel(node);
        functionBlock.Container.Should().Be(container);
    }

    // --- AddLabel ---------------------------------------------------------------------------------

    [Fact]
    public void AddLabel_RegistersNode_AppliesDefaultGeometryContentAndColors()
    {
        // Act
        var node = _sut.AddLabel(new Point(5, 6), zIndex: 7);

        // Assert
        node.Should().NotBeNull();
        var label = _state.DataflowDiagramMapping.GetModel(node);
        label.Container.Should().Be(_state.ActiveContainer);
        label.BackColor.Should().Be(LabelColors.BackgroundDefault);
        label.BorderColor.Should().Be(LabelColors.BorderDefault);
        label.Content.Should().Be(LabelDefaults.Content);
        label.Width.Should().Be(LabelDefaults.Width);
        label.Height.Should().Be(LabelDefaults.Height);
        label.ZIndex.Should().Be(7);
        label.X.Should().Be(5);
        label.Y.Should().Be(6);
    }

    // --- AddLink ----------------------------------------------------------------------------------

    [Fact]
    public async Task AddLink_WhenConnectionIsAllowed_CreatesLinkRegistersMapping_AndReturnsTrue()
    {
        // Arrange
        var (_, source) = await AddFunctionBlockAsync(new Point(0, 0));
        var (_, target) = await AddFunctionBlockAsync(new Point(300, 0));
        var nodeLink = CreateNodeLink(source, target);

        // Act
        var result = _sut.AddLink(nodeLink);

        // Assert
        result.Should().BeTrue();
        _state.DataflowDiagramMapping.GetNodeLinks().Should().Contain(nodeLink);
    }

    [Fact]
    public async Task AddLink_WhenConnectionIsNotAllowed_ReturnsFalse_AndDoesNotRegister()
    {
        // Arrange - occupy the target input first; inputs accept a single link, so a second one is rejected.
        var (_, source) = await AddFunctionBlockAsync(new Point(0, 0));
        var (_, target) = await AddFunctionBlockAsync(new Point(300, 0));
        _builder.Editors.Connector.AddLink(source.SystemOutputs.First(), target.SystemInputs.First());
        var nodeLink = CreateNodeLink(source, target);

        // Act
        var result = _sut.AddLink(nodeLink);

        // Assert
        result.Should().BeFalse();
        _state.DataflowDiagramMapping.GetNodeLinks().Should().NotContain(nodeLink);
    }

    // --- Remove -----------------------------------------------------------------------------------

    [Fact]
    public async Task Remove_FunctionBlockNode_RemovesMapping_AndRaisesFunctionBlockRemoved()
    {
        // Arrange
        var (node, functionBlock) = await AddFunctionBlockAsync(new Point(0, 0));
        var removedRaised = false;
        _diagramEvents.FunctionBlockRemoved += () => removedRaised = true;

        // Act
        _sut.Remove(node);

        // Assert
        _state.DataflowDiagramMapping.TryGetDiagramModel(functionBlock, out _).Should().BeFalse();
        _state.ActiveContainer.FunctionBlocks.Should().NotContain(functionBlock);
        removedRaised.Should().BeTrue();
    }

    [Fact]
    public async Task Remove_ChildContainerNode_RemovesMapping_AndRaisesContainerRemoved()
    {
        // Arrange
        var node = await _sut.AddChildContainer(_diagramService, new Point(50, 50), Ct);
        var container = _state.DataflowDiagramMapping.GetModel(node);
        ChildContainer? removed = null;
        _diagramEvents.ContainerRemoved += c => removed = c;

        // Act
        _sut.Remove(node);

        // Assert
        _state.DataflowDiagramMapping.TryGetDiagramModel(container, out _).Should().BeFalse();
        _state.ActiveContainer.Containers.Should().NotContain(container);
        removed.Should().BeSameAs(container);
    }

    [Fact]
    public void Remove_LabelNode_RemovesMappingAndModel()
    {
        // Arrange
        var node = _sut.AddLabel(new Point(5, 6), zIndex: 0);
        var label = _state.DataflowDiagramMapping.GetModel(node);

        // Act
        _sut.Remove(node);

        // Assert
        _state.DataflowDiagramMapping.TryGetDiagramModel(label, out _).Should().BeFalse();
        _state.ActiveContainer.Labels.Should().NotContain(label);
    }

    [Fact]
    public async Task Remove_BlockNodeLink_RemovesMapping()
    {
        // Arrange
        var (_, source) = await AddFunctionBlockAsync(new Point(0, 0));
        var (_, target) = await AddFunctionBlockAsync(new Point(300, 0));
        var nodeLink = CreateNodeLink(source, target);
        _sut.AddLink(nodeLink).Should().BeTrue();

        // Act
        _sut.Remove(nodeLink);

        // Assert
        _state.DataflowDiagramMapping.GetNodeLinks().Should().NotContain(nodeLink);
    }

    [Fact]
    public async Task RemoveMapping_RemovesConnectorMapping()
    {
        // Arrange
        var (_, functionBlock) = await AddFunctionBlockAsync(new Point(0, 0));
        var connector = functionBlock.SystemOutputs.First();
        var connectorNode = _state.DataflowDiagramMapping.GetDiagramModel(connector);

        // Act
        _sut.RemoveMapping(connectorNode);

        // Assert
        _state.DataflowDiagramMapping.TryGetDiagramModel(connector, out _).Should().BeFalse();
    }

    // --- Dataflow delegation ----------------------------------------------------------------------

    [Fact]
    public void AddDataflow_AddsNewDataflowNamedDataflow()
    {
        // Arrange
        var before = _builder.Cluster.Dataflows.Count;

        // Act
        _sut.AddDataflow();

        // Assert
        _builder.Cluster.Dataflows.Should().HaveCount(before + 1);
        _builder.Cluster.Dataflows.Should().Contain(d => d.Name == "Dataflow");
    }

    [Fact]
    public void RemoveDataflow_RemovesDataflowFromCluster()
    {
        // Arrange
        _sut.AddDataflow();
        var dataflow = _builder.Cluster.Dataflows.First(d => d.Name == "Dataflow");
        var before = _builder.Cluster.Dataflows.Count;

        // Act
        _sut.RemoveDataflow(dataflow);

        // Assert
        _builder.Cluster.Dataflows.Should().HaveCount(before - 1);
        _builder.Cluster.Dataflows.Should().NotContain(dataflow);
    }

    [Fact]
    public void SetDataflowName_UpdatesDataflowName()
    {
        // Arrange
        var dataflow = _state.ActiveDataflow;

        // Act
        _sut.SetDataflowName(dataflow, "Renamed");

        // Assert
        dataflow.Name.Should().Be("Renamed");
    }
}
