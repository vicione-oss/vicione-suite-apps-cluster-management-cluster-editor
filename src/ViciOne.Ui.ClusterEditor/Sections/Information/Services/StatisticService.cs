using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Sections.Information.Models;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.Information.Services;

internal sealed class StatisticService : IDisposable
{
    private ClusterBuilder? _clusterBuilder;
    private readonly ClusterBuilderEventBuffer _clusterBuilderEventBuffer;
    private readonly Datastore _datastore;

    public event Action<Statistic>? StatisticChanged;

    public StatisticService(ClusterBuilderEventBuffer clusterBuilderEventBuffer, Datastore datastore)
    {
        _clusterBuilderEventBuffer = clusterBuilderEventBuffer;
        _datastore = datastore;
        _datastore.BuilderChanged += OnBuilderChanged;
    }

    public void Dispose()
    {
        _datastore.BuilderChanged -= OnBuilderChanged;
        UnsubscribeBuilderEvents();
    }

    public Statistic GetStatistic()
    {
        if (_clusterBuilder is null)
            return new();

        var driverCount = _clusterBuilder.Cache.FunctionBlockDesigns
            .Select(kvp => _clusterBuilder.ResolveFunctionBlockDesign(kvp.Key))
            .Where(fbd => !string.IsNullOrEmpty(fbd.RuntimeDriverTypeName))
            .Select(fdb => fdb.RuntimeDriverTypeName)
            .Distinct()
            .Count();

        var links = _clusterBuilder.Cache.Links.Count(c => c.DestinationDataPortTreeNode is null && c.SourceDataPortTreeNode is null);
        var visibleLinks = _clusterBuilder.Cache.Links.Count(c => c.Visible);

        var dataPortTreeNodes = _clusterBuilder.Cache.DataPortTreeNodes.Count(c => c.ValueType is not null);
        var dataPortTreeNodesConnected = _clusterBuilder.Cache.DataPortTreeNodes.Count(c => c.Links.Any());

        var statistic = new Statistic()
        {
            Connectors = _clusterBuilder.Cache.Connectors.Count(c => c.Type is ConnectorType.System or ConnectorType.ProcessData),
            ConnectorsEventEnabled = _clusterBuilder.Cache.Connectors.Count(c => c.EventEnabled),
            ConnectorsPublished = _clusterBuilder.Cache.PublishedConnectors.Count,
            Containers = _clusterBuilder.Cache.Containers.Count,
            DataPorts = _clusterBuilder.Cache.DataPorts.Count,
            DataPortTreeNodes = dataPortTreeNodes,
            DataPortTreeNodesConnected = dataPortTreeNodesConnected,
            DataPortTreeNodesNotConnected = dataPortTreeNodes - dataPortTreeNodesConnected,
            Drivers = driverCount,
            Engines = _clusterBuilder.Cache.EngineGuids.Count,
            FbDesigns = _clusterBuilder.Cache.FunctionBlockDesigns.Count,
            Fbs = _clusterBuilder.Cache.FunctionBlocks.Count,
            Labels = _clusterBuilder.Cache.Labels.Count,
            Links = links,
            LinksHidden = links - visibleLinks,
            LinksVisible = visibleLinks,
        };

        return statistic;
    }

    private void InvokeStatisticChanged()
        => StatisticChanged?.Invoke(GetStatistic());

    private void OnBuilderChanged()
    {
        _clusterBuilder = _datastore.Builder;

        _clusterBuilderEventBuffer.ConnectorLinksAdded += OnClusterStateChanged;
        _clusterBuilderEventBuffer.ConnectorLinksRemoved += OnClusterStateChanged;
        _clusterBuilderEventBuffer.ConnectorPropertiesChanged += OnConnectorPropertiesChanged;
        _clusterBuilderEventBuffer.ContainersAdded += OnClusterStateChanged;
        _clusterBuilderEventBuffer.ContainersRemoved += OnClusterStateChanged;
        _clusterBuilderEventBuffer.DataflowsAdded += OnClusterStateChanged;
        _clusterBuilderEventBuffer.DataflowsRemoved += OnClusterStateChanged;
        _clusterBuilderEventBuffer.DataPortsAdded += OnClusterStateChanged;
        _clusterBuilderEventBuffer.DataPortsRemoved += OnClusterStateChanged;
        _clusterBuilderEventBuffer.EnginesAdded += OnClusterStateChanged;
        _clusterBuilderEventBuffer.EnginesRemoved += OnClusterStateChanged;
        _clusterBuilderEventBuffer.FunctionBlocksAdded += OnClusterStateChanged;
        _clusterBuilderEventBuffer.FunctionBlockDesignsAdded += OnClusterStateChanged;
        _clusterBuilderEventBuffer.FunctionBlockDesignsRemoved += OnClusterStateChanged;
        _clusterBuilderEventBuffer.FunctionBlocksRemoved += OnClusterStateChanged;
        _clusterBuilderEventBuffer.LabelsAdded += OnClusterStateChanged;
        _clusterBuilderEventBuffer.LabelsRemoved += OnClusterStateChanged;
        _clusterBuilderEventBuffer.TreeNodesAdded += OnClusterStateChanged;
        _clusterBuilderEventBuffer.TreeNodesRemoved += OnClusterStateChanged;

        InvokeStatisticChanged();
    }

    private void OnClusterStateChanged<T>(IEnumerable<T> _)
        => InvokeStatisticChanged();

    private void OnConnectorPropertiesChanged(IEnumerable<(object? sender, System.ComponentModel.PropertyChangedEventArgs e)> changedProperties)
    {
        foreach (var (_, e) in changedProperties)
        {
            if (e.PropertyName is (nameof(Connector.Published)) or (nameof(Connector.EventEnabled)))
                InvokeStatisticChanged();
        }
    }

    private void UnsubscribeBuilderEvents()
    {
        if (_clusterBuilderEventBuffer is not null)
        {
            _clusterBuilderEventBuffer.ConnectorLinksAdded -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.ConnectorLinksRemoved -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.ConnectorPropertiesChanged -= OnConnectorPropertiesChanged;
            _clusterBuilderEventBuffer.ContainersAdded -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.ContainersRemoved -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.DataflowsAdded -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.DataflowsRemoved -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.DataPortsAdded -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.DataPortsRemoved -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.EnginesAdded -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.EnginesRemoved -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.FunctionBlocksAdded -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.FunctionBlockDesignsAdded -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.FunctionBlockDesignsRemoved -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.FunctionBlocksRemoved -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.LabelsAdded -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.LabelsRemoved -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.TreeNodesAdded -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.TreeNodesRemoved -= OnClusterStateChanged;
        }
    }
}
