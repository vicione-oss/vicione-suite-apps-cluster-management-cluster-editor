using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Blazor.Diagrams.Core.Geometry;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using Connector = ViciOne.Cluster.Model.Connector;
using FunctionBlock = ViciOne.Cluster.Model.FunctionBlock;
using Link = ViciOne.Cluster.Model.Link;

namespace ViciOne.Ui.ClusterEditor.Services.ClusterServices;

public interface IDatastore : IDatastoreState, IAsyncDisposable
{
    event Action? ActiveDataflowChanged;
    event Action? BuilderChanged;
    event Action<BlockNodeLink>? ConnectorLinkRemoved;
    event Action<ChildContainer, string>? ContainerPropertyChanged;
    event Action? ForcedRefreshRequested;
    event Action<string>? PropertyChanged;

    Task<ChildContainerNode> AddChildContainer(DiagramService diagramService, Point position, CancellationToken cancellationToken = default, params IContainerChild[] children);
    void AddDataflow();
    Task<FunctionBlockNode> AddFunctionBlock(DiagramService diagramService, Guid designId, Point position, CancellationToken cancellationToken = default);
    LabelNode AddLabel(Point position, int zIndex);
    bool AddLink(BlockNodeLink nodeLink);
    Task DissolveContainerAsync(ChildContainer childContainer, DiagramService diagramService, SelectionManager selectionManager);
    IEnumerable<Link> GetConnectedLinks(BlockNode blockNode, ConnectionDirection direction, int? depth);
    IEnumerable<BlockNodeLink> GetConnectedNodeLinks(BlockNode blockNode, ConnectionDirection direction, int? depth);
    IEnumerable<BlockNodeLink> GetConnectedNodeLinks(IEnumerable<BlockNode> blockNodes);
    IEnumerable<BlockNode> GetConnectedNodes(IEnumerable<BlockNode> blockNodes);
    IEnumerable<BlockNodeConnector> GetValidTargetConnectors(IEnumerable<Connector> connectors, bool visibleLink);
    IConnector GetVisibleConnectorModel(BlockNodeConnector blockNodeConnector);
    IEnumerable<IConnector> GetVisibleConnectorModels(IEnumerable<BlockNodeConnector> blockNodeConnectors);
    Task Load(IClusterBuilder builder, DiagramService diagramService, CancellationToken cancellationToken);
    Task LoadContainer(Container container, DiagramService diagramService, CancellationToken? cancellationToken = null, bool force = false);
    Task MoveToNewContainerAsync(DiagramService diagramService, Point newContainerLocation, IEnumerable<FunctionBlock> functionBlocks, IEnumerable<ChildContainer> containers, IEnumerable<Label> labels, SelectionManager selectionManager);
    void Remove(ChildContainerNode containerNode);
    void Remove(FunctionBlockNode functionBlockNode);
    void Remove(BlockNodeLink nodeLink);
    void Remove(LabelNode labelNode);
    void RemoveDataflow(Dataflow dataflow);
    void RemoveMapping(BlockNodeConnector blockNodeConnector);
    void SaveViewport(DiagramService diagramService);
    void SetDataflowName(Dataflow dataflow, string newName);
}
