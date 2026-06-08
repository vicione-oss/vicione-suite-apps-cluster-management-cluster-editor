using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blazor.Diagrams.Core.Extensions;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Serialization.Json;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.Data;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.Shared.Dx.Services;
using Connector = ViciOne.Cluster.Model.Connector;
using FunctionBlock = ViciOne.Cluster.Model.FunctionBlock;
using Link = ViciOne.Cluster.Model.Link;

namespace ViciOne.Ui.ClusterEditor.Services;

[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling", Justification = "#1602")]
internal sealed partial class Datastore : IDatastore, IAsyncDisposable
{
    private IClusterBuilder? _builder;
    private readonly ClusterBuilderEventBuffer _clusterBuilderEventBuffer;
    private readonly ComparerService _comparerService;
    private readonly DiagramEventService _diagramEventService;
    private bool _disposed;
    private CancellationTokenSource? _dissolveContainerCts;
    private readonly SemaphoreSlim _dissolveContainerSemaphore = new(1);
    private readonly IJSRuntime _jsRuntime;
    private CancellationTokenSource? _loadContainerCts;
    private readonly SemaphoreSlim _loadContainerCtsSemaphore = new(1);
    private readonly ILogger<Datastore> _logger;

    public Cluster.Model.Container ActiveContainer { get; private set; } = new();
    public Dataflow ActiveDataflow { get; private set; } = new();
    public IClusterBuilder Builder => _builder ?? throw new InvalidOperationException($"Use method {nameof(Load)} to init the builder");
    public DataflowDiagramMapping DataflowDiagramMapping { get; } = new();
    public bool HasBuilder => _builder is not null;
    public IEnumerable<Cluster.Model.Engine> ValidDataflowEngines { get; private set; } = [];

    public event Action? ActiveDataflowChanged;
    public event Action? BuilderChanged;
    public event Action<BlockNodeLink>? ConnectorLinkRemoved;
    public event Action<ChildContainer, string>? ContainerPropertyChanged;
    public event Action? ForcedRefreshRequested;
    public event Action<string>? PropertyChanged;

    public Datastore(ClusterBuilderEventBuffer clusterBuilderEventBuffer, ComparerService comparerService, DiagramEventService diagramEventService, IJSRuntime jsRuntime, ILogger<Datastore> logger)
    {
        _clusterBuilderEventBuffer = clusterBuilderEventBuffer;
        _comparerService = comparerService;
        _diagramEventService = diagramEventService;
        _jsRuntime = jsRuntime;
        _logger = logger;
        JsonSerialization.Default.Settings.Formatting = Formatting.None;

        // This line adds the IAggregatingPooling to the cache of Shared.Dx.Services.ComparerService
        // which prevents a noticable delay when the user drags the first FunctionBlock from the Library
        // to the diagram
        var _ = _comparerService.GetComparer(typeof(Core.Contracts.DataModel.IAggregatingPooling));
    }

    private void AddBuilderEvents()
    {
        _clusterBuilderEventBuffer.ConnectorLinksAdded += OnConnectorLinksAdded;
        _clusterBuilderEventBuffer.ConnectorLinksRemoved += OnConnectorLinksRemoved;
        _clusterBuilderEventBuffer.ConnectorPropertiesChanged += OnConnectorPropertiesChanged;
        _clusterBuilderEventBuffer.ContainerPropertiesChanged += OnContainerPropertiesChanged;
        _clusterBuilderEventBuffer.EnginesAssigned += OnFunctionBlockEnginesChanged;
        _clusterBuilderEventBuffer.EnginesUnassigned += OnFunctionBlockEnginesChanged;
        _clusterBuilderEventBuffer.FunctionBlockPropertiesChanged += OnFunctionBlockPropertiesChanged;
        _clusterBuilderEventBuffer.LabelPropertiesChanged += OnLabelPropertiesChanged;
    }

    public async Task<ChildContainerNode> AddChildContainer(DiagramService diagramService, Point position, CancellationToken cancellationToken = default, params IContainerChild[] children)
    {
        RemoveBuilderEvents();

        var containerEditor = Builder.Editors.Container;
        var container = containerEditor.AddContainer(
            ActiveContainer,
            children: children,
            location: new((int)position.X, (int)position.Y)
        );
        containerEditor.SetBackColor(container, BlockNodeColors.BackgroundDefault);
        containerEditor.SetForeColor(container, BlockNodeColors.ForegroundDefault);

        var containerNodes = await AddChildContainersToMapping([container], diagramService, cancellationToken);

        AddBuilderEvents();
        _diagramEventService.InvokeContainerAdded(container);

        return containerNodes[0];
    }

    private async Task<List<ChildContainerNode>> AddChildContainersToMapping(List<ChildContainer> childContainers, DiagramService diagramService, CancellationToken cancellationToken)
    {
        if (childContainers.Count == 0)
            return [];

        var childContainerNames = new List<string>(childContainers.Count);
        foreach (var cc in childContainers)
            childContainerNames.Add(cc.Name);

        var measuredHeights = await _jsRuntime.MeasureNameFieldHeights(childContainerNames, cancellationToken);

        var result = new List<ChildContainerNode>(childContainers.Count);
        for (var i = 0; i < childContainers.Count; i++)
        {
            var node = ChildContainerMapper.CreateNode(_comparerService, this, diagramService, childContainers[i], measuredHeights[i]);
            DataflowDiagramMapping.Add(childContainers[i], node);

            result.Add(node);
        }

        return result;
    }

    public void AddDataflow()
        => Builder.Editors.Cluster.AddDataflow("Dataflow", Builder.Cluster.Version);

    public async Task<FunctionBlockNode> AddFunctionBlock(DiagramService diagramService, Guid designId, Point position, CancellationToken cancellationToken = default)
    {
        RemoveBuilderEvents();

        var functionBlock = Builder.Editors.Container.AddFunctionBlock(
            ActiveContainer,
            designId,
            location: new((int)position.X, (int)position.Y));

        var functionBlockEditor = Builder.Editors.FunctionBlock;
        functionBlockEditor.SetForeColor(functionBlock, BlockNodeColors.ForegroundDefault);
        functionBlockEditor.SetBackColor(functionBlock, BlockNodeColors.BackgroundDefault);

        var engine = ValidDataflowEngines.FirstOrDefault();
        if (engine is not null)
            functionBlockEditor.AssignEngine(engine, functionBlock);

        var functionBlockNodes = await AddFunctionBlocksToMapping([functionBlock], diagramService, cancellationToken);

        AddBuilderEvents();

        return functionBlockNodes[0];
    }

    private async Task<List<FunctionBlockNode>> AddFunctionBlocksToMapping(List<FunctionBlock> functionBlocks, DiagramService diagramService, CancellationToken cancellationToken)
    {
        if (functionBlocks.Count == 0)
            return [];

        var functionBlockNames = new List<string>(functionBlocks.Count);
        foreach (var fb in functionBlocks)
            functionBlockNames.Add(fb.Name);

        var measuredHeights = await _jsRuntime.MeasureNameFieldHeights(functionBlockNames, cancellationToken);

        var result = new List<FunctionBlockNode>(functionBlockNames.Count);
        for (var i = 0; i < functionBlockNames.Count; i++)
        {
            var node = FunctionBlockMapper.CreateNode(_comparerService, this, diagramService, functionBlocks[i], measuredHeights[i]);
            DataflowDiagramMapping.Add(functionBlocks[i], node);

            result.Add(node);
        }

        return result;
    }

    public LabelNode AddLabel(Point position, int zIndex)
    {
        RemoveBuilderEvents();

        var label = Builder.Editors.Container.AddLabel(
            ActiveContainer,
            new((int)position.X, (int)position.Y),
            new(LabelDefaults.Width, LabelDefaults.Height),
            zIndex,
            LabelDefaults.Content);

        var labelEditor = Builder.Editors.Label;
        labelEditor.SetBackColor(label, LabelColors.BackgroundDefault);
        labelEditor.SetBorderColor(label, LabelColors.BorderDefault);

        var labels = AddLabelsToMapping([label]);

        AddBuilderEvents();

        return labels[0];
    }

    private List<LabelNode> AddLabelsToMapping(List<Label> labels)
    {
        if (labels.Count == 0)
            return [];

        var result = new List<LabelNode>(labels.Count);

        foreach (var label in labels)
        {
            var node = LabelMapper.CreateNode(label);
            DataflowDiagramMapping.Add(label, node);

            result.Add(node);
        }

        return result;
    }

    public bool AddLink(BlockNodeLink nodeLink)
    {
        var sourceNodeConnector = (BlockNodeConnector)nodeLink.SourcePort!;
        var targetNodeConnector = (BlockNodeConnector)nodeLink.TargetPort!;
        var sourceConnector = (IConnectorOutput)DataflowDiagramMapping.GetModel(sourceNodeConnector);
        var targetConnector = (IConnectorInput)DataflowDiagramMapping.GetModel(targetNodeConnector);

        if (!Builder.Editors.Connector.CanCreateLink(sourceConnector, targetConnector))
            return false;

        var link = Builder.Editors.Connector.AddLink(sourceConnector, targetConnector);
        DataflowDiagramMapping.Add(link, nodeLink);

        return true;
    }

    private List<BlockNodeLink> AddLinksToMapping(List<Link> links)
    {
        if (links.Count == 0)
            return [];

        var result = new List<BlockNodeLink>(links.Count);

        foreach (var link in links)
        {
            var node = LinkMapper.CreateLink(this, link);
            DataflowDiagramMapping.Add(link, node);

            result.Add(node);
        }

        return result;
    }

    private Dictionary<FunctionBlock, List<Link>> BuildLinksByFbIndex(ConnectionDirection direction)
    {
        var linksByFb = new Dictionary<FunctionBlock, List<Link>>();
        foreach (var link in Builder.Cache.Links)
        {
            var fb = direction == ConnectionDirection.Predecessor
                ? link.DestinationConnector?.FunctionBlock
                : link.SourceConnector?.FunctionBlock;

            if (fb is null)
                continue;

            if (!linksByFb.TryGetValue(fb, out var list))
            {
                list = [];
                linksByFb[fb] = list;
            }

            list.Add(link);
        }

        return linksByFb;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposed, true, false))
            return;

        await _dissolveContainerSemaphore.WaitAsync();
        try
        {
            if (_dissolveContainerCts is not null)
            {
                await _dissolveContainerCts.CancelAsync();
                _dissolveContainerCts.Dispose();
            }
        }
        finally
        {
            _dissolveContainerSemaphore.Release();
        }

        _dissolveContainerSemaphore.Dispose();

        await _loadContainerCtsSemaphore.WaitAsync();
        try
        {
            if (_loadContainerCts is not null)
            {
                await _loadContainerCts.CancelAsync();
                _loadContainerCts.Dispose();
            }
        }
        finally
        {
            _loadContainerCtsSemaphore.Release();
        }

        _loadContainerCtsSemaphore.Dispose();
    }

    public async Task DissolveContainerAsync(ChildContainer childContainer, DiagramService diagramService, SelectionManager selectionManager)
    {
        if (_disposed)
            return;

        try
        {
            await _dissolveContainerSemaphore.WaitAsync();
            try
            {
                if (!DataflowDiagramMapping.TryGetDiagramModel(childContainer, out var childContainerNode))
                    return;

                _dissolveContainerCts = new();

                ModelDiagramMapper.RemoveNodesFromDiagram(
                    diagramService,
                    [childContainerNode,]);
                DataflowDiagramMapping.Remove(childContainer);

                foreach (var connector in childContainer.GetConnectors())
                {
                    foreach (var affectedLink in connector.Links)
                        DataflowDiagramMapping.Remove(affectedLink);
                }

                var containerPoint = new Point(
                    (childContainer.X ?? 0) + (BlockNodeLayout.Width / 2.0),
                    (childContainer.Y ?? 0) + (BlockNodeLayout.RowHeight * (BlockNodeLayout.SystemConnectorRows + BlockNodeLayout.MinimumConnectorRows) / 2)
                );
                var dissolvedChilds = Builder.Editors.Container.DissolveContainer(childContainer);
                var dissolvedFbs = new List<FunctionBlock>();
                var dissolvedContainers = new List<ChildContainer>();
                var dissolvedLabels = new List<Label>();

                foreach (var child in dissolvedChilds)
                {
                    if (child is FunctionBlock fb)
                        dissolvedFbs.Add(fb);
                    else if (child is ChildContainer cc)
                        dissolvedContainers.Add(cc);
                    else if (child is Label label)
                        dissolvedLabels.Add(label);
                }

                var newNodes = new List<NodeModel>(dissolvedFbs.Count + dissolvedContainers.Count + dissolvedLabels.Count);
                newNodes.AddRange(await AddFunctionBlocksToMapping(dissolvedFbs, diagramService, _dissolveContainerCts.Token));
                newNodes.AddRange(await AddChildContainersToMapping(dissolvedContainers, diagramService, _dissolveContainerCts.Token));
                newNodes.AddRange(AddLabelsToMapping(dissolvedLabels));

                var nodeCenter = newNodes.GetBounds().Center;
                var deltaX = containerPoint.X - nodeCenter.X;
                var deltaY = containerPoint.Y - nodeCenter.Y;

                foreach (var node in newNodes)
                {
                    node.SetPosition(node.Position.X + deltaX, node.Position.Y + deltaY);

                    if (node is ChildContainerNode ccNode)
                        ChildContainerMapper.UpdatePosition(this, ccNode);
                    else if (node is FunctionBlockNode fbNode)
                        FunctionBlockMapper.UpdatePosition(this, fbNode);
                    else if (node is LabelNode labelNode)
                        LabelMapper.UpdatePosition(this, labelNode);
                }

                var newLinks = new List<BlockNodeLink>();
                foreach (var link in GetActiveContainerFunctionBlockLinks().Concat(GetActiveContainerContainerLinks()))
                {
                    if (DataflowDiagramMapping.ContainsMapping(link))
                        continue;

                    var node = LinkMapper.CreateLink(this, link);
                    newLinks.Add(node);
                    DataflowDiagramMapping.Add(link, node);
                }

                ModelDiagramMapper.AddToDiagram(diagramService.Diagram, newNodes, newLinks);
                selectionManager.Select(newNodes.Cast<IDiagramModel>());
            }
            finally
            {
                _dissolveContainerCts?.Dispose();
                _dissolveContainerCts = null;

                _dissolveContainerSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // Nothing to do here, return gracefully
        }
        catch (ObjectDisposedException) when (_disposed)
        {
            // Semaphore or other object already disposed, nothing we can do, return gracefully
        }
    }

    private IEnumerable<Link> GetActiveContainerContainerLinks()
    {
        foreach (var container in ActiveContainer.Containers)
        {
            foreach (var connector in container.GetConnectors())
            {
                if (connector is not ContainerConnectorOutput output)
                    continue;

                foreach (var link in output.GetVisibleLinksConnectedToThis())
                    yield return link;
            }
        }
    }

    private IEnumerable<Link> GetActiveContainerFunctionBlockLinks()
    {
        foreach (var fb in ActiveContainer.FunctionBlocks)
        {
            foreach (var output in fb.GetAllOutputs())
            {
                foreach (var link in output.GetVisibleLinksConnectedToThis())
                    yield return link;
            }
        }
    }

    private HashSet<Link> GetConnectedLinks(FunctionBlock functionBlock, ConnectionDirection direction, int? depth)
        => GetConnectedLinks(functionBlock, direction, depth, BuildLinksByFbIndex(direction));

    private HashSet<Link> GetConnectedLinks(FunctionBlock functionBlock, ConnectionDirection direction, int? depth, Dictionary<FunctionBlock, List<Link>> linksByFb)
    {
        var result = new HashSet<Link>();
        var maxDepth = depth ?? int.MaxValue;
        var queue = new Queue<(int, FunctionBlock)>();
        var visited = new HashSet<FunctionBlock>();
        queue.Enqueue((1, functionBlock));

        while (queue.Count > 0)
        {
            var (currentDepth, fb) = queue.Dequeue();
            if (currentDepth > maxDepth)
                continue;

            if (!linksByFb.TryGetValue(fb, out var connectedLinks))
                continue;

            foreach (var link in connectedLinks)
            {
                result.Add(link);
                var nextFb = direction == ConnectionDirection.Predecessor ? link.SourceConnector?.FunctionBlock : link.DestinationConnector?.FunctionBlock;
                if (nextFb is not null && nextFb.Container == ActiveContainer && visited.Add(nextFb))
                    queue.Enqueue((currentDepth + 1, nextFb));
            }
        }

        return result;
    }

    private IEnumerable<Link> GetConnectedLinks(ChildContainer childContainer, ConnectionDirection direction, int? depth)
    {
        var linksByFb = BuildLinksByFbIndex(direction);
        var result = new HashSet<Link>();

        foreach (var connector in childContainer.GetConnectors())
            result.UnionWith(GetConnectedLinks(connector.FunctionBlock, direction, depth, linksByFb));

        return result;
    }

    public IEnumerable<Link> GetConnectedLinks(BlockNode blockNode, ConnectionDirection direction, int? depth)
    {
        if (blockNode is FunctionBlockNode fbNode)
            return GetConnectedLinks(DataflowDiagramMapping.GetModel(fbNode), direction, depth);

        if (blockNode is ChildContainerNode contNode)
            return GetConnectedLinks(DataflowDiagramMapping.GetModel(contNode), direction, depth);

        return [];
    }

    public IEnumerable<BlockNodeLink> GetConnectedNodeLinks(BlockNode blockNode, ConnectionDirection direction, int? depth)
        => DataflowDiagramMapping.GetDiagramModels(GetConnectedLinks(blockNode, direction, depth));

    public IEnumerable<BlockNodeLink> GetConnectedNodeLinks(IEnumerable<BlockNode> blockNodes)
    {
        var links = new HashSet<BlockNodeLink>();

        foreach (var node in blockNodes)
        {
            foreach (var link in GetConnectedNodeLinks(node, ConnectionDirection.Predecessor, 1))
                links.Add(link);

            foreach (var link in GetConnectedNodeLinks(node, ConnectionDirection.Successor, 1))
                links.Add(link);
        }

        return links;
    }

    public IEnumerable<BlockNode> GetConnectedNodes(IEnumerable<BlockNode> blockNodes)
    {
        var nodes = blockNodes.ToArray();
        var inputLinks = new HashSet<Link>();
        var outputLinks = new HashSet<Link>();

        foreach (var node in nodes)
        {
            if (node is FunctionBlockNode fbNode)
            {
                var functionBlock = DataflowDiagramMapping.GetModel(fbNode);
                foreach (var input in functionBlock.GetAllInputs())
                {
                    foreach (var link in input.Links)
                        inputLinks.Add(link);
                }

                foreach (var output in functionBlock.GetAllOutputs())
                {
                    foreach (var link in output.Links)
                        outputLinks.Add(link);
                }
            }
            else if (node is ChildContainerNode contNode)
            {
                var container = DataflowDiagramMapping.GetModel(contNode);
                foreach (var connector in container.GetConnectors())
                {
                    if (connector is ContainerConnectorInput input)
                    {
                        foreach (var link in input.Links)
                            inputLinks.Add(link);
                    }
                    else if (connector is ContainerConnectorOutput output)
                    {
                        foreach (var link in output.Links)
                            outputLinks.Add(link);
                    }
                }
            }
        }

        return GetConnectedNodes(inputLinks, outputLinks);
    }

    private List<BlockNode> GetConnectedNodes(HashSet<Link> inputLinks, HashSet<Link> outputLinks)
    {
        var connectedFbs = new HashSet<FunctionBlock>();
        var connectedContainers = new HashSet<ChildContainer>();

        foreach (var link in inputLinks)
        {
            if (link.SourceConnector is null)
                continue;

            if (link.SourceConnector.FunctionBlock.Container == ActiveContainer)
            {
                connectedFbs.Add(link.SourceConnector.FunctionBlock);
            }
            else
            {
                var upstreamContainer = link.SourceConnector.GetAllUpstreamContainerConnectors()
                    .FirstOrDefault(cc => cc.Container.Parent == ActiveContainer)?.Container;
                if (upstreamContainer is not null)
                    connectedContainers.Add(upstreamContainer);
            }
        }

        foreach (var link in outputLinks)
        {
            if (link.DestinationConnector is null)
                continue;

            if (link.DestinationConnector.FunctionBlock.Container == ActiveContainer)
            {
                connectedFbs.Add(link.DestinationConnector.FunctionBlock);
            }
            else
            {
                var upstreamContainer = link.DestinationConnector.GetAllUpstreamContainerConnectors()
                    .FirstOrDefault(cc => cc.Container.Parent == ActiveContainer)?.Container;
                if (upstreamContainer is not null)
                    connectedContainers.Add(upstreamContainer);
            }
        }

        var result = new List<BlockNode>(connectedFbs.Count + connectedContainers.Count);
        result.AddRange(DataflowDiagramMapping.GetDiagramModels(connectedFbs));
        result.AddRange(DataflowDiagramMapping.GetDiagramModels(connectedContainers));
        return result;
    }

    public IEnumerable<BlockNodeConnector> GetValidTargetConnectors(IEnumerable<Connector> connectors, bool visibleLink)
    {
        var connectorArray = connectors as Connector[] ?? [.. connectors];
        var isInputConnector = connectorArray[0] is IConnectorInput;

        var targetConnectors = isInputConnector
                ? DataflowDiagramMapping.GetOutputNodeConnectors().Select(DataflowDiagramMapping.GetModel)
                : DataflowDiagramMapping.GetInputNodeConnectors().Select(DataflowDiagramMapping.GetModel);

        targetConnectors = isInputConnector
            ? targetConnectors.Where(c => connectorArray.Any(sc => Builder.Editors.Connector.CanCreateLink((IConnectorOutput)c, (IConnectorInput)sc, visibleLink)))
            : targetConnectors.Where(c => connectorArray.Any(sc => Builder.Editors.Connector.CanCreateLink((IConnectorOutput)sc, (IConnectorInput)c, visibleLink)));

        return [.. targetConnectors.Select(DataflowDiagramMapping.GetDiagramModel)];
    }

    public IConnector GetVisibleConnectorModel(BlockNodeConnector blockNodeConnector)
    {
        if (blockNodeConnector.Node is ChildContainerNode)
        {
            var conModel = DataflowDiagramMapping.GetModel(blockNodeConnector);
            var upstreamContainerConnectors = conModel.GetUnderlyingConnector().GetAllUpstreamContainerConnectors();
            return upstreamContainerConnectors.First(cc => cc.Container.Parent == ActiveContainer);
        }
        else
        {
            return DataflowDiagramMapping.GetModel(blockNodeConnector);
        }
    }

    public IEnumerable<IConnector> GetVisibleConnectorModels(IEnumerable<BlockNodeConnector> blockNodeConnectors)
        => blockNodeConnectors.Select(GetVisibleConnectorModel);

    public async Task Load(IClusterBuilder builder, DiagramService diagramService, CancellationToken cancellationToken)
    {
        // Fast fail if cancellation has already been requested
        cancellationToken.ThrowIfCancellationRequested();

        _builder = builder;
        _clusterBuilderEventBuffer.SetBuilder(builder);

        BuilderChanged?.Invoke();

        var currentDataflow = Builder.Cluster.Dataflows[0];

        await LoadContainer(currentDataflow.Root, diagramService, cancellationToken);
    }

    public async Task LoadContainer(Cluster.Model.Container container, DiagramService diagramService, CancellationToken? externalCancellationToken = null, bool force = false)
    {
        // Dispose check needed because of possible race condition with DisposeAsync
        if (_disposed)
            return;

        if (externalCancellationToken is null)
        {
            // If there is no external cancellation token, we need to use the _loadContainerCts to cancel previous internal LoadContainer calls
            // if a new one is made before the previous one finishes. We have to do it internally here to coordinate all occurences where LoadContainer
            // is called, since the different callers don't know each other.
            try
            {
                await _loadContainerCtsSemaphore.WaitAsync();
                try
                {
                    // Cancel previous call to LoadContainer if there was one
                    if (_loadContainerCts is not null)
                    {
                        await _loadContainerCts.CancelAsync();
                        _loadContainerCts.Dispose();
                    }

                    _loadContainerCts = new CancellationTokenSource();
                    await LoadContainerSafely(container, diagramService, _loadContainerCts.Token, force);
                    _loadContainerCts = null;

                }
                finally
                {
                    _loadContainerCtsSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // Nothing to do here, return gracefully
            }
            catch (ObjectDisposedException) when (_disposed)
            {
                // Semaphore or other object already disposed, nothing we can do, return gracefully
            }
        }
        else
        {
            // If there is an external cancellation token, we assume that the caller is responsible for cancelling previous calls
            // to LoadContainer. We just cancel the last previous internal call if there is one. We also don't handle any Exceptions here because
            // the caller need to know about them and must handle them, since they are responsible for the cancellation in this case.
            await _loadContainerCtsSemaphore.WaitAsync();
            try
            {
                // Cancel previous internal call to LoadContainer if there was one
                if (_loadContainerCts is not null)
                {
                    await _loadContainerCts.CancelAsync();
                    _loadContainerCts.Dispose();
                }
                _loadContainerCts = null;

                await LoadContainerSafely(container, diagramService, externalCancellationToken.Value, force);
            }
            finally
            {
                _loadContainerCtsSemaphore.Release();
            }
        }
    }

    [LoggerMessage(1, LogLevel.Error, "Failed to load container {ContainerName}. {Message}", EventName = "LoadContainerFailed")]
    private static partial void LoadContainerFailed(ILogger logger, string containerName, string message);

    private async Task LoadContainerSafely(Cluster.Model.Container container, DiagramService diagramService, CancellationToken cancellationToken, bool force = false)
    {
        // Fast fail if cancellation has already been requested
        cancellationToken.ThrowIfCancellationRequested();

        if (ActiveContainer == container && !force)
            return;

        SaveViewport(ActiveContainer, diagramService.Diagram);

        ActiveContainer = container;
        var activeDataflow = container is ChildContainer childContainer
            ? Builder.Cache.GetDataflow(childContainer)
            : Builder.Cache.Dataflows.FirstOrDefault(x => x.Root == container);

        if (activeDataflow is null)
        {
            LoadContainerFailed(_logger, container.Name, "Dataflow not found in builder cache.");
            return;
        }

        if (activeDataflow != ActiveDataflow)
        {
            ActiveDataflow = activeDataflow;
            ActiveDataflowChanged?.Invoke();
        }

        ValidDataflowEngines = Builder.Cache.GetUsedEngines(ActiveDataflow).Concat(Builder.Cache.GetUnusedEngines());

        SearchBlocksEventService.RequestResetFindResult();

        RemoveBuilderEvents();
        diagramService.DiagramState.SuppressEvents = true;

        diagramService.Diagram.UnselectAll();
        diagramService.Diagram.Nodes.Clear();
        DataflowDiagramMapping.Clear();

        if (container.ViewportX.HasValue && container.ViewportY.HasValue && container.Zoom.HasValue)
        {
            diagramService.Diagram.SetPan(container.ViewportX.Value, container.ViewportY.Value);
            diagramService.Diagram.SetZoom(container.Zoom.Value);
        }

        await AddFunctionBlocksToMapping(container.FunctionBlocks, diagramService, cancellationToken);
        await AddChildContainersToMapping(container.Containers, diagramService, cancellationToken);
        AddLabelsToMapping(container.Labels);

        var links = new List<Link>();
        foreach (var link in GetActiveContainerFunctionBlockLinks())
            links.Add(link);
        foreach (var link in GetActiveContainerContainerLinks())
            links.Add(link);
        AddLinksToMapping(links);

        ModelDiagramMapper.AddToDiagram(diagramService.Diagram, DataflowDiagramMapping.GetNodes(), DataflowDiagramMapping.GetNodeLinks());

        AddBuilderEvents();
        diagramService.DiagramState.SuppressEvents = false;

        _diagramEventService.InvokeContainerLoaded(ActiveContainer);

        if (force)
            ForcedRefreshRequested?.Invoke();
    }

    public async Task MoveToNewContainerAsync(
        DiagramService diagramService,
        Point newContainerLocation,
        IEnumerable<FunctionBlock> functionBlocks,
        IEnumerable<ChildContainer> containers,
        IEnumerable<Label> labels,
        SelectionManager selectionManager)
    {
        var fbArray = functionBlocks.ToArray();
        var containerArray = containers.ToArray();
        var labelsArray = labels.ToArray();

        var affectedLinks = new List<Link>();
        foreach (var fb in fbArray)
        {
            foreach (var connector in fb.GetConnectors())
            {
                foreach (var link in connector.GetVisibleLinksConnectedToThis())
                    affectedLinks.Add(link);
            }
        }

        foreach (var container in containerArray)
        {
            foreach (var connector in container.GetConnectors())
            {
                foreach (var link in connector.GetVisibleLinksConnectedToThis())
                    affectedLinks.Add(link);
            }
        }

        ModelDiagramMapper.RemoveNodesFromDiagram(
            diagramService,
            DataflowDiagramMapping.GetDiagramModels(fbArray)
            .Cast<NodeModel>()
            .Concat(DataflowDiagramMapping.GetDiagramModels(containerArray))
            .Concat(DataflowDiagramMapping.GetDiagramModels(labelsArray))
        );

        foreach (var link in affectedLinks)
            DataflowDiagramMapping.Remove(link);
        foreach (var fb in fbArray)
            DataflowDiagramMapping.Remove(fb);
        foreach (var container in containerArray)
            DataflowDiagramMapping.Remove(container);
        foreach (var label in labelsArray)
            DataflowDiagramMapping.Remove(label);

        var containerNode = await AddChildContainer(
            diagramService,
            newContainerLocation,
            default,
            [.. fbArray.Cast<IContainerChild>(), .. containerArray, .. labelsArray]
        );

        var newLinks = new List<BlockNodeLink>();
        foreach (var link in GetActiveContainerFunctionBlockLinks().Concat(GetActiveContainerContainerLinks()))
        {
            if (DataflowDiagramMapping.ContainsMapping(link))
                continue;

            var node = LinkMapper.CreateLink(this, link);
            DataflowDiagramMapping.Add(link, node);
            newLinks.Add(node);
        }

        ModelDiagramMapper.AddToDiagram(diagramService.Diagram, [containerNode], newLinks);

        selectionManager.Select(containerNode);
    }

    private void OnConnectorLinksAdded(IEnumerable<Link> links)
    {
        foreach (var link in links)
        {
            if (link.Visible)
                continue;

            UpdateConnectorMarker(link);
        }
    }

    private void OnConnectorLinksRemoved(IEnumerable<Link> links)
    {
        foreach (var link in links)
        {
            if (!link.Visible)
                UpdateConnectorMarker(link);

            // refresh source connector properties to reflect link changes
            var sourceConnector = link.SourceConnector;
            if (sourceConnector is not null && DataflowDiagramMapping.TryGetDiagramModel(sourceConnector, out var diagramConnector))
            {
                diagramConnector!.SetHasUpstreamLinks(sourceConnector.HasUpstreamLinks());
                diagramConnector.Parent.Refresh();
            }

            // refresh destination connector properties to reflect link changes
            var destinationConnector = link.DestinationConnector;
            if (destinationConnector is not null && DataflowDiagramMapping.TryGetDiagramModel(destinationConnector, out diagramConnector))
            {
                diagramConnector!.SetHasUpstreamLinks(destinationConnector.HasUpstreamLinks());
                diagramConnector.Parent.Refresh();
            }

            if (DataflowDiagramMapping.TryGetDiagramModel(link, out var linkNode))
                ConnectorLinkRemoved?.Invoke(linkNode!);
        }
    }

    private void OnConnectorPropertiesChanged(IEnumerable<(object? sender, PropertyChangedEventArgs e)> changedConnectorProperties)
    {
        foreach (var (sender, e) in changedConnectorProperties)
        {
            if (e.PropertyName is null)
                continue;

            if (sender is not IConnector connector)
                continue;

            // Changing a FB setting raises this event twice for a [setting name] connector for the properties
            // "Value" and "ValueSerialized". We don't have a mapping for these connectors since we don't display
            // them or interact with them directly.
            if (!DataflowDiagramMapping.TryGetDiagramModel(connector, out var nodeConnector))
                continue;

            ConnectorMapper.PropertyChanged(connector, nodeConnector, e.PropertyName);
            PropertyChanged?.Invoke(e.PropertyName);
        }
    }

    private void OnContainerPropertiesChanged(IEnumerable<(object? sender, PropertyChangedEventArgs e)> changedContainerProperties)
    {
        foreach (var (sender, e) in changedContainerProperties)
        {
            if (sender is not ChildContainer childContainer
                || e.PropertyName is null
                || !DataflowDiagramMapping.TryGetDiagramModel(childContainer, out var childContainerNode))
            {
                continue;
            }

            ChildContainerMapper.PropertyChanged(childContainer, childContainerNode, this, e.PropertyName);
            PropertyChanged?.Invoke(e.PropertyName);
            ContainerPropertyChanged?.Invoke(childContainer, e.PropertyName);
        }
    }

    private void OnFunctionBlockEnginesChanged(IEnumerable<FunctionBlock> functionBlocks)
    {
        var fbs = functionBlocks.ToArray();
        List<FunctionBlock> downstreamFbs = [];

        foreach (var fb in fbs)
        {
            if (DataflowDiagramMapping.TryGetDiagramModel(fb, out var functionBlockNode))
                FunctionBlockMapper.PropertyChanged(this, fb, functionBlockNode, nameof(FunctionBlock.Engine));
            else
                downstreamFbs.Add(fb);
        }

        PropertyChanged?.Invoke(nameof(FunctionBlock.Engine));

        if (downstreamFbs.Count != 0)
        {
            // This can fail if we assign an engine to an FB because it's maybe not part of the ActiveContainer already
            var container = downstreamFbs[0].GetAllUpstreamContainers().FirstOrDefault(c => ActiveContainer.Containers.Contains(c));
            if (container is not null)
                OnContainerPropertiesChanged([(container, new(nameof(FunctionBlock.Engine)))]);
        }
    }

    private void OnFunctionBlockPropertiesChanged(IEnumerable<(object? sender, PropertyChangedEventArgs e)> changedFunctionBlockProperties)
    {
        foreach (var (sender, e) in changedFunctionBlockProperties)
        {
            if (sender is not FunctionBlock functionBlock
                || e.PropertyName is null
                || e.PropertyName == nameof(FunctionBlock.Engine)
                || !DataflowDiagramMapping.TryGetDiagramModel(functionBlock, out var functionBlockNode))
            {
                continue;
            }

            FunctionBlockMapper.PropertyChanged(this, functionBlock, functionBlockNode, e.PropertyName);
            PropertyChanged?.Invoke(e.PropertyName);
        }
    }

    private void OnLabelPropertiesChanged(IEnumerable<(object? sender, PropertyChangedEventArgs e)> changedLabelProperties)
    {
        foreach (var (sender, e) in changedLabelProperties)
        {
            if (sender is not Label label || e.PropertyName is null || !DataflowDiagramMapping.TryGetDiagramModel(label, out var labelNode))
                continue;

            LabelMapper.PropertyChanged(label, labelNode, e.PropertyName);
            PropertyChanged?.Invoke(e.PropertyName);
        }
    }

    public void Remove(ChildContainerNode containerNode)
    {
        var container = DataflowDiagramMapping.GetModel(containerNode);

        Builder.Editors.Container.RemoveContainer(container);
        DataflowDiagramMapping.Remove(container);

        _diagramEventService.InvokeContainerRemoved(container);
    }

    public void Remove(FunctionBlockNode functionBlockNode)
    {
        var functionBlock = DataflowDiagramMapping.GetModel(functionBlockNode);

        Builder.Editors.Container.RemoveFunctionBlock(functionBlock);
        DataflowDiagramMapping.Remove(functionBlock);

        _diagramEventService.InvokeFunctionBlockRemoved();
    }

    public void Remove(BlockNodeLink nodeLink)
    {
        var link = DataflowDiagramMapping.GetModel(nodeLink);

        Builder.Editors.Connector.RemoveLink(link);
        DataflowDiagramMapping.Remove(link);
    }

    public void Remove(LabelNode labelNode)
    {
        var label = DataflowDiagramMapping.GetModel(labelNode);

        Builder.Editors.Container.RemoveLabel(label);
        DataflowDiagramMapping.Remove(label);
    }

    private void RemoveBuilderEvents()
    {
        _clusterBuilderEventBuffer.ConnectorLinksAdded -= OnConnectorLinksAdded;
        _clusterBuilderEventBuffer.ConnectorLinksRemoved -= OnConnectorLinksRemoved;
        _clusterBuilderEventBuffer.ConnectorPropertiesChanged -= OnConnectorPropertiesChanged;
        _clusterBuilderEventBuffer.ContainerPropertiesChanged -= OnContainerPropertiesChanged;
        _clusterBuilderEventBuffer.EnginesAssigned -= OnFunctionBlockEnginesChanged;
        _clusterBuilderEventBuffer.EnginesUnassigned -= OnFunctionBlockEnginesChanged;
        _clusterBuilderEventBuffer.FunctionBlockPropertiesChanged -= OnFunctionBlockPropertiesChanged;
        _clusterBuilderEventBuffer.LabelPropertiesChanged -= OnLabelPropertiesChanged;
    }

    public void RemoveDataflow(Dataflow dataflow)
        => Builder.Editors.Cluster.RemoveDataflow(dataflow);

    public void RemoveMapping(BlockNodeConnector blockNodeConnector)
    {
        var connector = DataflowDiagramMapping.GetModel(blockNodeConnector);
        DataflowDiagramMapping.Remove(connector);
    }

    public void SaveViewport(DiagramService diagramService)
        => SaveViewport(ActiveContainer, diagramService.Diagram);

    private static void SaveViewport(Cluster.Model.Container container, global::Blazor.Diagrams.Core.Diagram diagram)
    {
        container.ViewportX = diagram.Pan.X;
        container.ViewportY = diagram.Pan.Y;
        container.Zoom = diagram.Zoom;
    }

    public void SetDataflowName(Dataflow dataflow, string newName)
        => Builder.Editors.Dataflow.SetName(dataflow, newName);

    private void UpdateConnectorMarker(Link link)
    {
        if (link.SourceConnector is not null && DataflowDiagramMapping.TryGetDiagramModel(link.SourceConnector, out var sourceConnectorNode))
            sourceConnectorNode.UpdateConnectorMarker();

        if (link.DestinationConnector is not null && DataflowDiagramMapping.TryGetDiagramModel(link.DestinationConnector, out var targetConnectorNode))
            targetConnectorNode.UpdateConnectorMarker();
    }
}
