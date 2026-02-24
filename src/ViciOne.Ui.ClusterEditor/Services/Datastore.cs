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
using ViciOne.Cluster.Builder;
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

[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling", Justification = "TODO")]
public sealed partial class Datastore : IAsyncDisposable
{
    private ClusterBuilder? _builder;
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
    public ClusterBuilder Builder => _builder ?? throw new InvalidOperationException($"Use method {nameof(Load)} to init the builder");
    internal DataflowDiagramMapping DataflowDiagramMapping { get; } = new();
    internal bool HasBuilder => _builder is not null;
    public IEnumerable<Cluster.Model.Engine> ValidDataflowEngines { get; private set; } = [];

    public event Action? ActiveDataflowChanged;
    public event Action? BuilderChanged;
    public event Action<BlockNodeLink>? ConnectorLinkRemoved;
    public event Action<ChildContainer, string>? ContainerPropertyChanged;
    public event Action? ForcedRefreshRequested;
    public event Action? NodesChanged;
    public event Action<string>? PropertyChanged;

    public Datastore(ClusterBuilderEventBuffer clusterBuilderEventBuffer, ComparerService comparerService, DiagramEventService diagramEventService, IJSRuntime jsRuntime, ILogger<Datastore> logger)
    {
        _clusterBuilderEventBuffer = clusterBuilderEventBuffer;
        _comparerService = comparerService;
        _diagramEventService = diagramEventService;
        _jsRuntime = jsRuntime;
        _logger = logger;
        JsonSerialization.Default.Settings.Formatting = Formatting.None;
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

    public async Task<ChildContainerNode> AddContainerAsync(DiagramService diagramService, Point position, CancellationToken cancellationToken = default, params IContainerChild[] children)
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

        var containerNode = await ChildContainerMapper.CreateNodeAsync(_comparerService, container, this, diagramService, _jsRuntime, cancellationToken);
        DataflowDiagramMapping.Add(container, containerNode);

        AddBuilderEvents();
        _diagramEventService.InvokeContainerAdded(container);

        return containerNode;
    }

    public void AddDataflow()
        => Builder.Editors.Cluster.AddDataflow("Dataflow", Builder.Cluster.Version);

    public async Task<FunctionBlockNode> AddFunctionBlockAsync(DiagramService diagramService, Guid designId, Point position, CancellationToken cancellationToken = default)
    {
        RemoveBuilderEvents();

        var functionBlock = Builder.Editors.Container.AddFunctionBlock(
            ActiveContainer,
            designId,
            location: new((int)position.X, (int)position.Y));

        var functionBlockEditor = Builder.Editors.FunctionBlock;
        functionBlockEditor.SetForeColor(functionBlock, BlockNodeColors.ForegroundDefault);
        functionBlockEditor.SetBackColor(functionBlock, BlockNodeColors.BackgroundDefault);
        if (ValidDataflowEngines.Any())
            functionBlockEditor.AssignEngine(ValidDataflowEngines.First(), functionBlock);

        var functionBlockNode = await FunctionBlockMapper.CreateNodeAsync(_comparerService, this, diagramService, functionBlock, _jsRuntime, cancellationToken);
        DataflowDiagramMapping.Add(functionBlock, functionBlockNode);

        AddBuilderEvents();

        return functionBlockNode;
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

        var labelNode = LabelMapper.CreateNode(label);
        DataflowDiagramMapping.Add(label, labelNode);

        AddBuilderEvents();

        return labelNode;
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

                foreach (var affectedLink in childContainer.GetConnectors().SelectMany(c => c.Links))
                    DataflowDiagramMapping.Remove(affectedLink);

                var containerPoint = new Point(
                    (childContainer.X ?? 0) + (BlockNodeLayout.Width / 2.0),
                    (childContainer.Y ?? 0) + (BlockNodeLayout.RowHeight * (BlockNodeLayout.SystemConnectorRows + BlockNodeLayout.MinimumConnectorRows) / 2)
                );
                var dissolvedChilds = Builder.Editors.Container.DissolveContainer(childContainer);

                var resultNodes = new List<NodeModel>();
                foreach (var child in dissolvedChilds)
                {
                    if (child is FunctionBlock functionBlock)
                    {
                        var node = await FunctionBlockMapper.CreateNodeAsync(_comparerService, this, diagramService, functionBlock, _jsRuntime, _dissolveContainerCts.Token);
                        resultNodes.Add(node);
                        DataflowDiagramMapping.Add(functionBlock, node);
                    }
                    else if (child is ChildContainer container)
                    {
                        var node = await ChildContainerMapper.CreateNodeAsync(_comparerService, container, this, diagramService, _jsRuntime, _dissolveContainerCts.Token);
                        resultNodes.Add(node);
                        DataflowDiagramMapping.Add(container, node);
                    }
                    else if (child is Label label)
                    {
                        var node = LabelMapper.CreateNode(label);
                        resultNodes.Add(node);
                        DataflowDiagramMapping.Add(label, node);
                    }
                }

                var nodeCenter = resultNodes.GetBounds().Center;
                var deltaX = containerPoint.X - nodeCenter.X;
                var deltaY = containerPoint.Y - nodeCenter.Y;

                foreach (var node in resultNodes)
                {
                    node.SetPosition(node.Position.X + deltaX, node.Position.Y + deltaY);

                    if (node is ChildContainerNode ccNode)
                        ChildContainerMapper.UpdatePosition(this, ccNode);
                    else if (node is FunctionBlockNode fbNode)
                        FunctionBlockMapper.UpdatePosition(this, fbNode);
                    else if (node is LabelNode labelNode)
                        LabelMapper.UpdatePosition(this, labelNode);
                }

                var links = GetActiveContainerFunctionBlockLinks()
                    .Concat(GetActiveContainerContainerLinks()).Where(l => !DataflowDiagramMapping.ContainsMapping(l));
                var newLinks = new List<BlockNodeLink>();
                foreach (var link in links)
                {
                    var node = LinkMapper.CreateLink(this, link);
                    newLinks.Add(node);
                    DataflowDiagramMapping.Add(link, node);
                }

                ModelDiagramMapper.AddToDiagram(diagramService.Diagram, resultNodes, newLinks);
                selectionManager.Select(resultNodes.Cast<IDiagramModel>());
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
        => ActiveContainer.Containers
            .SelectMany(c => c.GetConnectors().OfType<ContainerConnectorOutput>())
            .SelectMany(con => con.GetVisibleLinksConnectedToThis());

    private IEnumerable<Link> GetActiveContainerFunctionBlockLinks()
        => ActiveContainer.FunctionBlocks
            .SelectMany(fb => fb.GetAllOutputs())
            .SelectMany(con => con.GetVisibleLinksConnectedToThis());

    private HashSet<Link> GetConnectedLinks(FunctionBlock functionBlock, ConnectionDirection direction, int? depth)
    {
        var result = new HashSet<Link>();
        var maxDepth = depth ?? int.MaxValue;
        var queue = new Queue<(int, FunctionBlock)>();
        var alreadyDoneFbs = new HashSet<FunctionBlock>();
        queue.Enqueue((1, functionBlock));

        while (queue.Count > 0)
        {
            var (currentDepth, fb) = queue.Dequeue();
            if (currentDepth > maxDepth || alreadyDoneFbs.Contains(fb))
                continue;

            var connectedLinks = Builder.Cache.Links
                .Where(l => (direction == ConnectionDirection.Predecessor ? l.DestinationConnector?.FunctionBlock : l.SourceConnector?.FunctionBlock) == fb);

            foreach (var link in connectedLinks)
            {
                result.Add(link);
                var nextFb = direction == ConnectionDirection.Predecessor ? link.SourceConnector?.FunctionBlock : link.DestinationConnector?.FunctionBlock;
                if (nextFb is not null && nextFb.Container == ActiveContainer && !queue.Contains((currentDepth + 1, nextFb)))
                    queue.Enqueue((currentDepth + 1, nextFb));
            }

            alreadyDoneFbs.Add(fb);
        }

        return result;
    }

    private IEnumerable<Link> GetConnectedLinks(ChildContainer childContainer, ConnectionDirection direction, int? depth)
        => childContainer.GetConnectors()
            .Select(c => c.FunctionBlock)
            .SelectMany(fb => GetConnectedLinks(fb, direction, depth));

    public IEnumerable<Link> GetConnectedLinks(BlockNode blockNode, ConnectionDirection direction, int? depth)
    {
        var links = new HashSet<Link>();
        if (blockNode is FunctionBlockNode fbNode)
        {
            links.UnionWith(GetConnectedLinks(DataflowDiagramMapping.GetModel(fbNode), direction, depth));
        }
        else if (blockNode is ChildContainerNode contNode)
        {
            links.UnionWith(GetConnectedLinks(DataflowDiagramMapping.GetModel(contNode), direction, depth));
        }

        return links;
    }

    public IEnumerable<BlockNodeLink> GetConnectedNodeLinks(BlockNode blockNode, ConnectionDirection direction, int? depth)
        => DataflowDiagramMapping.GetDiagramModels(GetConnectedLinks(blockNode, direction, depth));

    public IEnumerable<BlockNodeLink> GetConnectedNodeLinks(IEnumerable<BlockNode> blockNodes)
    {
        var links = new HashSet<BlockNodeLink>();
        var connectedLinksPre = blockNodes
            .SelectMany(n => GetConnectedNodeLinks(n, ConnectionDirection.Predecessor, 1));
        var connectedLinksSucc = blockNodes
            .SelectMany(n => GetConnectedNodeLinks(n, ConnectionDirection.Successor, 1));

        links.UnionWith(connectedLinksPre);
        links.UnionWith(connectedLinksSucc);
        return links;
    }

    public IEnumerable<BlockNode> GetConnectedNodes(IEnumerable<BlockNode> blockNodes)
    {
        var nodes = blockNodes.ToArray();
        var inputLinks = new HashSet<Link>();
        var outputLinks = new HashSet<Link>();

        foreach (var fbNode in nodes.OfType<FunctionBlockNode>())
        {
            var functionBlock = DataflowDiagramMapping.GetModel(fbNode);
            inputLinks.UnionWith(functionBlock.GetAllInputs().SelectMany(c => c.Links));
            outputLinks.UnionWith(functionBlock.GetAllOutputs().SelectMany(c => c.Links));
        }

        foreach (var contNode in nodes.OfType<ChildContainerNode>())
        {
            var container = DataflowDiagramMapping.GetModel(contNode);
            inputLinks.UnionWith(container.GetConnectors().OfType<ContainerConnectorInput>().SelectMany(c => c.Links));
            outputLinks.UnionWith(container.GetConnectors().OfType<ContainerConnectorOutput>().SelectMany(c => c.Links));
        }

        return GetConnectedNodes(inputLinks, outputLinks);
    }

    private IEnumerable<BlockNode> GetConnectedNodes(HashSet<Link> inputLinks, HashSet<Link> outputLinks)
    {
        var connectedFbs = new HashSet<FunctionBlock>();
        var connectedContainers = new HashSet<ChildContainer>();

        var inputConnectedContainers = inputLinks
            .Where(l => l.SourceConnector is not null)
            .Where(l => l.SourceConnector!.FunctionBlock.Container != ActiveContainer)
            .Where(l => l.SourceConnector!.GetAllUpstreamContainerConnectors().Any(cc => cc.Container.Parent == ActiveContainer))
            .Select(l => l.SourceConnector!.GetAllUpstreamContainerConnectors().First(cc => cc.Container.Parent == ActiveContainer).Container);
        var inputConnectedFbs = inputLinks
            .Where(l => l.SourceConnector is not null)
            .Where(l => l.SourceConnector!.FunctionBlock.Container == ActiveContainer)
            .Select(l => l.SourceConnector!.FunctionBlock);

        var outputConnectedContainers = outputLinks
            .Where(l => l.DestinationConnector is not null)
            .Where(l => l.DestinationConnector!.FunctionBlock.Container != ActiveContainer)
            .Where(l => l.DestinationConnector!.GetAllUpstreamContainerConnectors().Any(cc => cc.Container.Parent == ActiveContainer))
            .Select(l => l.DestinationConnector!.GetAllUpstreamContainerConnectors().First(cc => cc.Container.Parent == ActiveContainer).Container);
        var outputConnectedFbs = outputLinks
            .Where(l => l.DestinationConnector is not null)
            .Where(l => l.DestinationConnector!.FunctionBlock.Container == ActiveContainer)
            .Select(l => l.DestinationConnector!.FunctionBlock);

        connectedContainers.UnionWith(inputConnectedContainers);
        connectedFbs.UnionWith(inputConnectedFbs);
        connectedContainers.UnionWith(outputConnectedContainers);
        connectedFbs.UnionWith(outputConnectedFbs);

        return Enumerable.Empty<BlockNode>()
            .Concat(DataflowDiagramMapping.GetDiagramModels(connectedFbs))
            .Concat(DataflowDiagramMapping.GetDiagramModels(connectedContainers));
    }

    public IEnumerable<BlockNodeConnector> GetValidTargetConnectors(IEnumerable<Connector> connectors, bool visibleLink)
    {
        var connector = connectors.First();

        var connectorType = Builder.DetermineValueType(connector);

        var isInputConnector = connector is IConnectorInput;
        var targetConnectors = isInputConnector
                ? DataflowDiagramMapping.GetOutputNodeConnectors().Select(DataflowDiagramMapping.GetModel)
                : DataflowDiagramMapping.GetInputNodeConnectors().Select(DataflowDiagramMapping.GetModel);

        targetConnectors = isInputConnector
            ? targetConnectors.Where(c => connectors.Any(sc => Builder.Editors.Connector.CanCreateLink((IConnectorOutput)c, (IConnectorInput)sc, visibleLink)))
            : targetConnectors.Where(c => connectors.Any(sc => Builder.Editors.Connector.CanCreateLink((IConnectorOutput)sc, (IConnectorInput)c, visibleLink)));

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

    public async Task Load(ClusterBuilder builder, DiagramService diagramService)
    {
        _builder = builder;
        _clusterBuilderEventBuffer.SetBuilder(builder);

        BuilderChanged?.Invoke();

        var currentDataflow = Builder.Cluster.Dataflows.First();

        await LoadContainer(currentDataflow.Root, diagramService);
    }

    public async Task LoadContainer(Cluster.Model.Container container, DiagramService diagramService, bool force = false)
    {
        // Dispose check needed because of possible race condition with DisposeAsync
        if (_disposed)
            return;

        try
        {
            var cancellationToken = CancellationToken.None;

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

                cancellationToken = _loadContainerCts.Token;
            }
            finally
            {
                _loadContainerCtsSemaphore.Release();
            }

            await LoadContainerSafely(container, diagramService, cancellationToken, force);
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

    [LoggerMessage(1, LogLevel.Error, "Failed to load container {ContainerName}. {Message}", EventName = "LoadContainerFailed")]
    private static partial void LoadContainerFailed(ILogger logger, string containerName, string message);

    private async Task LoadContainerSafely(Cluster.Model.Container container, DiagramService diagramService, CancellationToken cancellationToken, bool force = false)
    {
        if (ActiveContainer == container && !force)
            return;

        diagramService.Diagram.UnselectAll();

        diagramService.DiagramState.SuppressEvents = true;
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

        RemoveBuilderEvents();
        NodesChanged?.Invoke();
        diagramService.Diagram.Nodes.Clear();
        DataflowDiagramMapping.Clear();
        SearchBlocksEventService.RequestResetFindResult();

        foreach (var functionBlock in container.FunctionBlocks)
        {
            var node = await FunctionBlockMapper.CreateNodeAsync(_comparerService, this, diagramService, functionBlock, _jsRuntime, cancellationToken);
            DataflowDiagramMapping.Add(functionBlock, node);
        }

        foreach (var containerChild in container.Containers)
        {
            var node = await ChildContainerMapper.CreateNodeAsync(_comparerService, containerChild, this, diagramService, _jsRuntime, cancellationToken);
            DataflowDiagramMapping.Add(containerChild, node);
        }

        foreach (var label in container.Labels)
        {
            var node = LabelMapper.CreateNode(label);
            DataflowDiagramMapping.Add(label, node);
        }

        var links = GetActiveContainerFunctionBlockLinks().Concat(GetActiveContainerContainerLinks());
        foreach (var link in links)
        {
            var node = LinkMapper.CreateLink(this, link);
            DataflowDiagramMapping.Add(link, node);
        }

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

        var affectedLinks = fbArray.SelectMany(fb => fb.GetConnectors().SelectMany(c => c.GetVisibleLinksConnectedToThis()))
            .Concat(containerArray.SelectMany(c => c.GetConnectors().SelectMany(c => c.GetVisibleLinksConnectedToThis())))
            .ToArray();

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

        var containerNode = await AddContainerAsync(
            diagramService,
            newContainerLocation,
            default,
            [.. fbArray.Cast<IContainerChild>(), .. containerArray, .. labelsArray]
        );

        var links = GetActiveContainerFunctionBlockLinks()
            .Concat(GetActiveContainerContainerLinks()).Where(l => !DataflowDiagramMapping.ContainsMapping(l));
        var newLinks = new List<BlockNodeLink>();
        foreach (var link in links)
        {
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
            var container = downstreamFbs.First().GetAllUpstreamContainers().FirstOrDefault(c => ActiveContainer.Containers.Contains(c));
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
