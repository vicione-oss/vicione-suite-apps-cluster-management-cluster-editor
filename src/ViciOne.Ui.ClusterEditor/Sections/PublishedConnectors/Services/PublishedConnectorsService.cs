using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;

namespace ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class PublishedConnectorsService : IDisposable
{
    private readonly ClusterBuilderEventBuffer _clusterBuilderEventBuffer;
    private readonly IDatastore _datastore;
    private readonly List<DataGridConnectorWrapper> _publishedConnectorWrappers = [];

    /// <summary>
    /// The published connectors, as a snapshot that is replaced rather than mutated whenever the set changes.
    /// Wrapper instances are reused across snapshots, so a selection held elsewhere keeps its identity.
    /// </summary>
    public IReadOnlyList<DataGridConnectorWrapper> PublishedConnectorWrappers { get; private set; } = [];

    public event Action? PublishedConnectorsChanged;

    /// <summary>
    /// Raised when something outside the section — a double click on a published connector marker in the
    /// diagram — asks for a connector's row to become the section's selection.
    /// </summary>
    public event Action<IConnector>? PublishedConnectorSelectionRequested;

    public PublishedConnectorsService(ClusterBuilderEventBuffer clusterBuilderEventBuffer, IDatastore datastore)
    {
        _clusterBuilderEventBuffer = clusterBuilderEventBuffer;
        _clusterBuilderEventBuffer.ConnectorLinksAdded += OnConnectorLinksChanged;
        _clusterBuilderEventBuffer.ConnectorLinksRemoved += OnConnectorLinksChanged;
        _clusterBuilderEventBuffer.ConnectorPropertiesChanged += OnConnectorPropertiesChanged;
        _clusterBuilderEventBuffer.ContainersChanged += OnClusterStateChanged;
        _clusterBuilderEventBuffer.ContainerPropertiesChanged += OnClusterStateChanged;
        _clusterBuilderEventBuffer.DataflowPropertiesChanged += OnClusterStateChanged;
        _clusterBuilderEventBuffer.FunctionBlockPropertiesChanged += OnFunctionBlockPropertiesChanged;
        _clusterBuilderEventBuffer.FunctionBlocksRemoved += OnClusterStateChanged;

        _datastore = datastore;
        _datastore.BuilderChanged += OnClusterBuilderChanged;
    }

    /// <summary>
    /// Links every published connector in <paramref name="publishedConnectorWrappers"/> to the connector behind
    /// <paramref name="targetBlockNodeConnector"/>, skipping the pairs the builder rejects.
    /// </summary>
    /// <remarks>
    /// A wrapper may name a connector unpublished after the drag started, so every pair is validated.
    /// </remarks>
    internal void AddLinks(IReadOnlyList<DataGridConnectorWrapper> publishedConnectorWrappers,
        BlockNodeConnector targetBlockNodeConnector)
    {
        var secondConnector = _datastore.DataflowDiagramMapping.GetModel(targetBlockNodeConnector);

        foreach (var publishedConnector in publishedConnectorWrappers)
        {
            var sourceConnector = (IConnectorOutput)(publishedConnector.IsInput ? secondConnector : publishedConnector.Connector);
            var targetConnector = (IConnectorInput)(publishedConnector.IsInput ? publishedConnector.Connector : secondConnector);

            if (_datastore.Builder.Editors.Connector.CanCreateLink(sourceConnector, targetConnector, false))
                _datastore.Builder.Editors.Connector.AddLink(sourceConnector, targetConnector, false);
        }
    }

    private static int CompareConnectorWrappers(DataGridConnectorWrapper? x, DataGridConnectorWrapper? y)
    {
        if (x is null || y is null)
            return 0;

        var pathCompare = string.Compare(x.Path, y.Path, StringComparison.Ordinal);
        if (pathCompare != 0)
            return pathCompare;

        var fbCompare = string.Compare(x.FunctionBlockName, y.FunctionBlockName, StringComparison.Ordinal);
        if (fbCompare != 0)
            return fbCompare;

        var inputCompare = x.IsInput.CompareTo(y.IsInput);
        if (inputCompare != 0)
            return inputCompare;

        return string.Compare(x.ConnectorName, y.ConnectorName, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        _clusterBuilderEventBuffer.ConnectorLinksAdded -= OnConnectorLinksChanged;
        _clusterBuilderEventBuffer.ConnectorLinksRemoved -= OnConnectorLinksChanged;
        _clusterBuilderEventBuffer.ConnectorPropertiesChanged -= OnConnectorPropertiesChanged;
        _clusterBuilderEventBuffer.ContainersChanged -= OnClusterStateChanged;
        _clusterBuilderEventBuffer.ContainerPropertiesChanged -= OnClusterStateChanged;
        _clusterBuilderEventBuffer.DataflowPropertiesChanged -= OnClusterStateChanged;
        _clusterBuilderEventBuffer.FunctionBlockPropertiesChanged -= OnFunctionBlockPropertiesChanged;
        _clusterBuilderEventBuffer.FunctionBlocksRemoved -= OnClusterStateChanged;

        _datastore.BuilderChanged -= OnClusterBuilderChanged;
    }

    /// <summary>
    /// The name of the dataflow <paramref name="connector"/>'s function block belongs to.
    /// </summary>
    private string? GetDataflowName(IConnector connector)
        => _datastore.Builder.Cache.GetDataflow(connector.FunctionBlock)?.Name;


    /// <summary>
    /// The connectors that every published connector in <paramref name="publishedConnectorWrappers"/> may legally
    /// be linked to.
    /// </summary>
    internal IReadOnlySet<BlockNodeConnector> GetValidTargetConnectors(IReadOnlyList<DataGridConnectorWrapper> publishedConnectorWrappers)
    {
        List<Connector> connectors = [];
        foreach (var wrapper in publishedConnectorWrappers)
        {
            if (wrapper.Connector is Connector connector)
                connectors.Add(connector);
        }

        return new HashSet<BlockNodeConnector>(_datastore.GetValidTargetConnectors(connectors, false));
    }

    private Task OnClusterBuilderChanged()
    {
        // A reloaded cluster hands out new connector instances under the same ids, so a wrapper matched by id
        // would keep pointing into the cluster that was replaced.
        _publishedConnectorWrappers.Clear();

        UpdatePublishedConnectorEntries();
        return Task.CompletedTask;
    }

    private void OnClusterStateChanged<T>(IEnumerable<T> _)
        => UpdatePublishedConnectorEntries();

    private void OnConnectorLinksChanged(IEnumerable<Link> links)
    {
        var updateNeeded = false;

        foreach (var link in links)
        {
            if (link.Visible)
                continue;

            updateNeeded = true;
            break;
        }

        if (updateNeeded)
            UpdatePublishedConnectorEntries();
    }

    private void OnConnectorPropertiesChanged(IEnumerable<(object? sender, PropertyChangedEventArgs e)> changedConnectorProperties)
    {
        var updateNeeded = false;

        foreach (var (sender, e) in changedConnectorProperties)
        {
            if (e.PropertyName is null)
                continue;

            switch (e.PropertyName)
            {
                case nameof(Connector.Published):
                case nameof(Connector.Description):
                    updateNeeded = true;
                    break;
            }

            if (updateNeeded)
                break;
        }

        if (updateNeeded)
            UpdatePublishedConnectorEntries();
    }

    private void OnFunctionBlockPropertiesChanged(IEnumerable<(object? Sender, PropertyChangedEventArgs EventArgs)> functionBlockProperties)
    {
        var updateNeeded = false;

        foreach (var (sender, e) in functionBlockProperties)
        {
            if (e.PropertyName is null)
                continue;

            if (sender is not FunctionBlock functionBlock)
                continue;

            if (e.PropertyName == nameof(FunctionBlock.Name))
            {
                foreach (var c in _datastore.Builder.Cache.PublishedConnectors)
                {
                    if (c.FunctionBlock == functionBlock)
                    {
                        updateNeeded = true;
                        break;
                    }
                }

                if (updateNeeded)
                    break;
            }
        }

        if (updateNeeded)
            UpdatePublishedConnectorEntries();
    }

    internal void RemovePublishedConnectors(IEnumerable<DataGridConnectorWrapper>? publishedConnectorsWrappers)
    {
        if (publishedConnectorsWrappers is null)
            return;

        foreach (var publishedConnector in publishedConnectorsWrappers)
            _datastore.Builder.Editors.Connector.SetPublished((Connector)publishedConnector.Connector, false);
    }

    /// <summary>
    /// Asks the section to make <paramref name="connector"/>'s row its selection.
    /// </summary>
    /// <remarks>
    /// Always the underlying connector: a container's marker is a proxy and has no row of its own.
    /// </remarks>
    public void RequestPublishedConnectorSelection(IConnector connector)
        => PublishedConnectorSelectionRequested?.Invoke(connector);

    private void UpdatePublishedConnectorEntries()
    {
        var publishedConnectors = _datastore.Builder.Cache.PublishedConnectors;

        var currentlyPublishedIds = new HashSet<Guid>(publishedConnectors.Count);
        foreach (var pc in publishedConnectors)
            currentlyPublishedIds.Add(pc.Id);

        var existingIds = new HashSet<Guid>(_publishedConnectorWrappers.Count);
        foreach (var pc in _publishedConnectorWrappers)
            existingIds.Add(pc.Connector.Id);

        _publishedConnectorWrappers.RemoveAll(pc => !currentlyPublishedIds.Contains(pc.Connector.Id));

        // A surviving wrapper keeps its instance so the table's selection survives, so nothing else ever rebuilds
        // it — its path has to be re-resolved here or a dataflow, container or function-block rename would never
        // reach the Path column.
        foreach (var pc in _publishedConnectorWrappers)
            pc.RefreshPath(GetDataflowName(pc.Connector));

        foreach (var pc in publishedConnectors)
        {
            if (!existingIds.Contains(pc.Id))
            {
                _publishedConnectorWrappers.Add(new DataGridConnectorWrapper(
                    pc,
                    _datastore.Builder.ResolveConnectorDesign(pc),
                    _datastore.Builder.ResolveFunctionBlockDesign(pc.FunctionBlock.DesignId),
                    dataflowName: GetDataflowName(pc)
                ));
            }
        }

        _publishedConnectorWrappers.Sort(CompareConnectorWrappers);

        PublishedConnectorWrappers = [.. _publishedConnectorWrappers];

        PublishedConnectorsChanged?.Invoke();
    }
}
