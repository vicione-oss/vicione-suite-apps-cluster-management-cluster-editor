using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Blazor.Diagrams.Core.Geometry;
using Newtonsoft.Json;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Serialization.Json;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.Data;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using Connector = ViciOne.Cluster.Model.Connector;
using FunctionBlock = ViciOne.Cluster.Model.FunctionBlock;
using Link = ViciOne.Cluster.Model.Link;

namespace ViciOne.Ui.ClusterEditor.Services.ClusterServices;

/// <summary>
/// Thin facade for the cluster editor state and services that
/// implement loading, editing, restructuring and querying of cluster elements.
/// </summary>
internal sealed class Datastore : IDatastore
{
    private readonly ClusterEditService _editService;
    private readonly ContainerLoadService _loadService;
    private readonly LinkQueryService _queryService;
    private readonly ContainerRestructureService _restructureService;
    private readonly DatastoreState _state;

    public Container ActiveContainer => _state.ActiveContainer;
    public Dataflow ActiveDataflow => _state.ActiveDataflow;
    public IClusterBuilder Builder => _state.Builder;
    public DataflowDiagramMapping DataflowDiagramMapping => _state.DataflowDiagramMapping;
    public bool HasBuilder => _state.HasBuilder;
    public IEnumerable<Cluster.Model.Engine> ValidDataflowEngines => _state.ValidDataflowEngines;

    public event Action? ActiveDataflowChanged
    {
        add => _state.ActiveDataflowChanged += value;
        remove => _state.ActiveDataflowChanged -= value;
    }

    public event Func<Task>? BuilderChanged
    {
        add => _state.BuilderChanged += value;
        remove => _state.BuilderChanged -= value;
    }

    public event Action<BlockNodeLink>? ConnectorLinkRemoved
    {
        add => _state.ConnectorLinkRemoved += value;
        remove => _state.ConnectorLinkRemoved -= value;
    }

    public event Action<ChildContainer, string>? ContainerPropertyChanged
    {
        add => _state.ContainerPropertyChanged += value;
        remove => _state.ContainerPropertyChanged -= value;
    }

    public event Func<Task>? ForcedRefreshRequested
    {
        add => _state.ForcedRefreshRequested += value;
        remove => _state.ForcedRefreshRequested -= value;
    }

    public event Action<string>? PropertyChanged
    {
        add => _state.PropertyChanged += value;
        remove => _state.PropertyChanged -= value;
    }

    public Datastore(
        ClusterEditService editService,
        ContainerLoadService loadService,
        LinkQueryService queryService,
        ContainerRestructureService restructureService,
        DatastoreState state)
    {
        _editService = editService;
        _loadService = loadService;
        _queryService = queryService;
        _restructureService = restructureService;
        _state = state;

        JsonSerialization.Default.Settings.Formatting = Formatting.None;
    }

    public Task<ChildContainerNode> AddChildContainer(DiagramService diagramService, Point position, CancellationToken cancellationToken = default, params IContainerChild[] children)
        => _editService.AddChildContainer(diagramService, position, cancellationToken, children);

    public void AddDataflow()
        => _editService.AddDataflow();

    public Task<FunctionBlockNode> AddFunctionBlock(DiagramService diagramService, Guid designId, Point position, CancellationToken cancellationToken = default)
        => _editService.AddFunctionBlock(diagramService, designId, position, cancellationToken);

    public LabelNode AddLabel(Point position, int zIndex)
        => _editService.AddLabel(position, zIndex);

    public bool AddLink(BlockNodeLink nodeLink)
        => _editService.AddLink(nodeLink);

    public async ValueTask DisposeAsync()
    {
        await _restructureService.DisposeAsync();
        await _loadService.DisposeAsync();
    }

    public Task DissolveContainerAsync(ChildContainer childContainer, DiagramService diagramService, SelectionManager selectionManager)
        => _restructureService.DissolveContainerAsync(childContainer, diagramService, selectionManager);

    public IEnumerable<Link> GetConnectedLinks(BlockNode blockNode, ConnectionDirection direction, int? depth)
        => _queryService.GetConnectedLinks(blockNode, direction, depth);

    public IEnumerable<BlockNodeLink> GetConnectedNodeLinks(BlockNode blockNode, ConnectionDirection direction, int? depth)
        => _queryService.GetConnectedNodeLinks(blockNode, direction, depth);

    public IEnumerable<BlockNodeLink> GetConnectedNodeLinks(IEnumerable<BlockNode> blockNodes)
        => _queryService.GetConnectedNodeLinks(blockNodes);

    public IEnumerable<BlockNode> GetConnectedNodes(IEnumerable<BlockNode> blockNodes)
        => _queryService.GetConnectedNodes(blockNodes);

    public IEnumerable<BlockNodeConnector> GetValidTargetConnectors(IEnumerable<Connector> connectors, bool visibleLink)
        => _queryService.GetValidTargetConnectors(connectors, visibleLink);

    public IConnector GetVisibleConnectorModel(BlockNodeConnector blockNodeConnector)
        => _queryService.GetVisibleConnectorModel(blockNodeConnector);

    public IEnumerable<IConnector> GetVisibleConnectorModels(IEnumerable<BlockNodeConnector> blockNodeConnectors)
        => _queryService.GetVisibleConnectorModels(blockNodeConnectors);

    public Task Load(IClusterBuilder builder, DiagramService diagramService, CancellationToken cancellationToken)
        => _loadService.Load(builder, diagramService, cancellationToken);

    public Task LoadContainer(Container container, DiagramService diagramService, CancellationToken? cancellationToken = null, bool force = false)
        => _loadService.LoadContainer(container, diagramService, cancellationToken, force);

    public Task MoveToNewContainerAsync(DiagramService diagramService, Point newContainerLocation, IEnumerable<FunctionBlock> functionBlocks, IEnumerable<ChildContainer> containers, IEnumerable<Label> labels, SelectionManager selectionManager)
        => _restructureService.MoveToNewContainerAsync(diagramService, newContainerLocation, functionBlocks, containers, labels, selectionManager);

    public void Remove(ChildContainerNode containerNode)
        => _editService.Remove(containerNode);

    public void Remove(FunctionBlockNode functionBlockNode)
        => _editService.Remove(functionBlockNode);

    public void Remove(BlockNodeLink nodeLink)
        => _editService.Remove(nodeLink);

    public void Remove(LabelNode labelNode)
        => _editService.Remove(labelNode);

    public void RemoveDataflow(Dataflow dataflow)
        => _editService.RemoveDataflow(dataflow);

    public void RemoveMapping(BlockNodeConnector blockNodeConnector)
        => _editService.RemoveMapping(blockNodeConnector);

    public void SaveViewport(DiagramService diagramService)
        => _loadService.SaveViewport(diagramService);

    public void SetDataflowName(Dataflow dataflow, string newName)
        => _editService.SetDataflowName(dataflow, newName);
}
