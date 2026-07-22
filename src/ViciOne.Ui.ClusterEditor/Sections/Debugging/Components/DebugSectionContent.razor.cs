#if DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.TreeBuilder.NodeTypes;
using ViciOne.TreeBuilder.Rules;
using ViciOne.Ui.Blazor.Components.ComboBox;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Localization;
using ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.Localization.Resources;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;
using TechnicalTerms = ViciOne.Ui.ClusterEditor.Localization.Resources.TechnicalTerms;

namespace ViciOne.Ui.ClusterEditor.Sections.Debugging.Components;

[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling", Justification = "Debug")]
public sealed partial class DebugSectionContent : ComponentBase
{
    private const int MaxConsecutiveFailures = 20;

    private static readonly string s_openInNewWindow = MonochromeIconName.OpenInNewWindow.GetCssClasses(MonochromeIconSize.Small).ToSpaceSeparated();

    private bool _debugPopupVisible;
    private readonly string _generateAllFbsText = CompositeFormats.GenerateSomething($"{CommonVocabulary.All} {TechnicalTerms.FunctionBlockPlural}");
    private int _generateBlocksAmount = 25;
    private string _generateBlocksName = "TwoWaySelector";
    private int _generateDataPointsAmount = 1;
    private int _generateLinksAmount = 1;
    private readonly List<ComboBoxItem<GridMode, string>> _gridModeComboBoxItems = [..
        Enum.GetValues<GridMode>()
            .Select(gridMode
                => new ComboBoxItem<GridMode, string>
                {
                    Text = gridMode.ToString(),
                    Value = gridMode,
                })
    ];
    private readonly Random _rnd = new();

    [Inject] private ClusterBuilderEventBuffer ClusterBuilderEventBuffer { get; set; } = default!;
    [Inject] private IContextMenuSettings ContextMenuSettings { get; set; } = default!;
    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private DiagramService DiagramService { get; set; } = default!;
    [Inject] private IRulesetProvider RulesetProvider { get; set; } = default!;

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is only debug data")]
    private List<NodeReference>? FindPathToDataPoint(NodeReference[] candidates, IReadOnlyDictionary<string, NodeType> nodeTypes)
    {
        foreach (var candidate in candidates.OrderBy(_ => _rnd.Next()))
        {
            if (!nodeTypes.TryGetValue(candidate.Id, out var nodeType))
                continue;

            if (nodeType.IsDataPoint())
                return [candidate];

            var childPath = FindPathToDataPoint(nodeType.ChildNodes, nodeTypes);
            if (childPath is not null)
                return [candidate, .. childPath];
        }

        return null;
    }

    private async Task GenerateBlocks(IEnumerable<Guid> uniqueIds, int amount)
    {
        ClusterBuilderEventBuffer.StartBatchOpertation();

        var blockNodes = new List<FunctionBlockNode>();
        foreach (var (id, idx) in uniqueIds.Select((id, idx) => (id, idx)))
        {
            for (var i = 0; i < amount; i++)
            {
                var blockNode = await Datastore.AddFunctionBlock(
                    DiagramService,
                    id,
                    new(
                        ((idx * amount) + i) % 5 * 320,
                        ((idx * amount) + i) / 5 * 280));

                blockNodes.Add(blockNode);
            }
        }

        DiagramService.Diagram.Nodes.Add(blockNodes);

        ClusterBuilderEventBuffer.EndBatchOperation();
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is only debug data")]
    private bool GenerateDataPoint()
    {
        var builder = Datastore.Builder;

        var rulesetIds = RulesetProvider.GetRulesetIdentifiers(DataPortTreeAdapter.DataPortCategory).ToList();
        if (rulesetIds.Count == 0)
            return false;

        var ruleset = RulesetProvider.GetRuleset(rulesetIds[_rnd.Next(rulesetIds.Count)]);
        if (ruleset?.Root is null)
            return false;

        var nodeTypes = ruleset.NodeTypes.ToDictionary(n => n.Id);

        // Pick a random DataPort type that can reach a leaf DataPoint and build the path of tree nodes leading to it.
        List<NodeReference>? treeNodePath = null;
        NodeType? dataPortNodeType = null;
        foreach (var dataPortRef in ruleset.Root.ChildNodes.OrderBy(_ => _rnd.Next()))
        {
            if (!nodeTypes.TryGetValue(dataPortRef.Id, out var candidate))
                continue;

            treeNodePath = FindPathToDataPoint(candidate.ChildNodes, nodeTypes);
            if (treeNodePath is not null)
            {
                dataPortNodeType = candidate;
                break;
            }
        }

        if (dataPortNodeType is null || treeNodePath is null)
            return false;

        builder.EnsureSystemDataPortDependencyExists(RulesetProvider);

        var dataPort = builder.Editors.Dataflow.AddDataPort(
            Datastore.ActiveDataflow,
            dataPortNodeType.Id,
            dataPortNodeType.Name,
            PickDirection(dataPortNodeType),
            ruleset.Root.Id);

        DataPortTreeNode? current = null;
        foreach (var nodeRef in treeNodePath)
        {
            var nodeType = nodeTypes[nodeRef.Id];
            var valueType = nodeType.IsDataPoint() ? PickRandomValueType(nodeType, ruleset) : null;
            var transferMode = PickTransferMode(nodeType);

            current = current is null
                ? builder.Editors.DataPort.AddTreeNode(nodeType.Id, dataPort, nodeType.Name, valueType, transferMode)
                : builder.Editors.DataPortTreeNode.AddTreeNode(nodeType.Id, current, nodeType.Name, valueType, transferMode);
        }

        return true;
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is only debug data")]
    private void GenerateDataPortLinks(int amount)
    {
        var dataPortEditor = Datastore.Builder.Editors.DataPortTreeNode;
        var cache = Datastore.Builder.Cache;

        var dataPortTreeNodes = cache.DataPortTreeNodes.Where(n => n.ValueType is not null).ToList();
        var candidateConnectors = cache.Connectors.Where(c => c.Type != ConnectorType.Setting).ToList();
        if (dataPortTreeNodes.Count == 0 || candidateConnectors.Count == 0)
            return;

        var created = 0;
        var consecutiveFailures = 0;
        while (created < amount && consecutiveFailures < MaxConsecutiveFailures)
        {
            var dataPortTreeNode = dataPortTreeNodes[_rnd.Next(dataPortTreeNodes.Count)];

            var validConnectors = candidateConnectors
                .Where(c => dataPortEditor.CanAssignConnector(dataPortTreeNode, c))
                .ToList();
            if (validConnectors.Count == 0)
            {
                consecutiveFailures++;
                continue;
            }

            var connector = validConnectors[_rnd.Next(validConnectors.Count)];
            dataPortEditor.AssignConnector(dataPortTreeNode, connector);

            consecutiveFailures = 0;
            created++;
        }
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is only debug data")]
    private void GenerateHiddenLinks(int amount)
    {
        var connectorEditor = Datastore.Builder.Editors.Connector;
        var allConnectors = Datastore.Builder.Cache.Connectors.Where(c => c.Type != ConnectorType.Setting).ToList();

        var outputConnectors = allConnectors.OfType<IConnectorOutput>().ToList();
        var inputConnectors = allConnectors.OfType<IConnectorInput>().ToList();
        if (outputConnectors.Count == 0 || inputConnectors.Count == 0)
            return;

        var created = 0;
        var consecutiveFailures = 0;
        while (created < amount && consecutiveFailures < MaxConsecutiveFailures)
        {
            var sourceConnector = outputConnectors[_rnd.Next(outputConnectors.Count)];

            var validDestinationConnectors = inputConnectors
                .Where(c => connectorEditor.CanCreateLink(sourceConnector, c, false))
                .ToList();
            if (validDestinationConnectors.Count == 0)
            {
                consecutiveFailures++;
                continue;
            }

            var destinationConnector = validDestinationConnectors[_rnd.Next(validDestinationConnectors.Count)];

            connectorEditor.SetPublished(_rnd.NextDouble() < 0.5 ? sourceConnector : destinationConnector, true);
            connectorEditor.AddLink(sourceConnector, destinationConnector, false);

            consecutiveFailures = 0;
            created++;
        }
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is only debug data")]
    private void GenerateVisibleLinks(int amount)
    {
        var connectorEditor = Datastore.Builder.Editors.Connector;
        var visibleBlocks = Datastore.ActiveContainer.FunctionBlocks;
        var visibleContainers = Datastore.ActiveContainer.Containers;

        var outputConnectors = visibleBlocks.SelectMany(b => b.Outputs)
            .Union(visibleContainers.SelectMany(c => c.GetConnectors().OfType<IConnectorOutput>())).ToList();
        var inputConnectors = visibleBlocks.SelectMany(b => b.Inputs)
            .Union(visibleContainers.SelectMany(c => c.GetConnectors().OfType<IConnectorInput>())).ToList();
        if (outputConnectors.Count == 0 || inputConnectors.Count == 0)
            return;

        var created = 0;
        var consecutiveFailures = 0;
        while (created < amount && consecutiveFailures < MaxConsecutiveFailures)
        {
            var sourceConnector = outputConnectors[_rnd.Next(outputConnectors.Count)];

            var validDestinationConnectors = inputConnectors
                .Where(c => connectorEditor.CanCreateLink(sourceConnector, c, true))
                .ToList();
            if (validDestinationConnectors.Count == 0)
            {
                consecutiveFailures++;
                continue;
            }

            var destinationConnector = validDestinationConnectors[_rnd.Next(validDestinationConnectors.Count)];

            var link = connectorEditor.AddLink(sourceConnector, destinationConnector);
            var nodeLink = LinkMapper.CreateLink(Datastore, link);
            Datastore.DataflowDiagramMapping.Add(link, nodeLink);
            DiagramService.Diagram.Links.Add(nodeLink);

            consecutiveFailures = 0;
            created++;
        }
    }

    private async Task OnGenerateAllLibraryBlocksClickAsync()
        => await GenerateBlocks(Datastore.Builder.GetFunctionBlockDesigns().Select(d => d.Id), 1);

    private async Task OnGenerateBlocksClickAsync()
    {
        var design = Datastore.Builder.GetFunctionBlockDesigns()
            .FirstOrDefault(d => string.Equals(d.Name, _generateBlocksName, StringComparison.OrdinalIgnoreCase));
        if (design is null)
            return;

        await GenerateBlocks([design.Id], _generateBlocksAmount);
    }

    private void OnGenerateDataPointsClick()
    {
        if (_generateDataPointsAmount < 1)
            return;

        var targetAmount = _generateDataPointsAmount;
        var currentAmount = 0;
        var availableTries = targetAmount + 20;

        ClusterBuilderEventBuffer.StartBatchOpertation();

        while (currentAmount < targetAmount && availableTries > 0)
        {
            if (GenerateDataPoint())
                currentAmount++;
            else
                availableTries--;
        }

        ClusterBuilderEventBuffer.EndBatchOperation();

        if (currentAmount > 0)
            Datastore.RequestForcedRefresh();
    }

    private void OnGenerateLinksClick(GenerateLinksType type)
    {
        if (_generateLinksAmount < 1)
            return;

        ClusterBuilderEventBuffer.StartBatchOpertation();

        switch (type)
        {
            case GenerateLinksType.Visible:
                GenerateVisibleLinks(_generateLinksAmount);
                break;
            case GenerateLinksType.Hidden:
                GenerateHiddenLinks(_generateLinksAmount);
                break;
            case GenerateLinksType.DataPort:
                GenerateDataPortLinks(_generateLinksAmount);
                break;
        }

        ClusterBuilderEventBuffer.EndBatchOperation();
    }

    private void OnGridModeChanged(GridMode item)
        => DiagramService.RequestGridModeChange(item);

    private void OnNodeAlignmentBorderVisibleChanged(bool nodeAlignmentBorderVisible)
        => DiagramService.SetNodeAlignmentBorderActive(nodeAlignmentBorderVisible);

    private void OnShowDebugConsoleButtonClick()
        => _debugPopupVisible = true;

    private void OnUseGimpPanBehaviorChanged(bool useGimpPanBehavior)
        => DiagramService.RequestPanBehaviorChange(useGimpPanBehavior);

    private void OnUseMinimapNodeColorsChanged(bool useMinimapNodeColors)
        => DiagramService.SetMinimapNodeColoring(useMinimapNodeColors);

    private void OnUseSimplifiedViewChanged(bool useSimplyfiedView)
        => DiagramService.RequestSimplifiedViewChange(useSimplyfiedView);

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is only debug data")]
    private DataPortDirection PickDirection(NodeType dataPortNodeType)
    {
        var hasInbound = dataPortNodeType.TransferDirections.Contains(DataPortTransferDirection.Inbound);
        var hasOutbound = dataPortNodeType.TransferDirections.Contains(DataPortTransferDirection.Outbound);

        if (hasInbound && hasOutbound)
            return (DataPortDirection)_rnd.Next(3);
        if (hasInbound)
            return DataPortDirection.In;
        return DataPortDirection.Out;
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is only debug data")]
    private Type? PickRandomValueType(NodeType leafNodeType, Ruleset ruleset)
    {
        var runtimeTypes = leafNodeType.DataTypes
            .Select(name => ruleset.DataTypes.FirstOrDefault(d => d.Name == name)?.RuntimeType)
            .Where(type => type is not null)
            .ToList();

        return runtimeTypes.Count == 0 ? null : runtimeTypes[_rnd.Next(runtimeTypes.Count)];
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is only debug data")]
    private Cluster.Model.DataPortTransferMode PickTransferMode(NodeType nodeType)
    {
        if (nodeType is DataPortTreeNodeType treeNodeType && treeNodeType.TransferModes.Length > 0)
        {
            var mode = treeNodeType.TransferModes[_rnd.Next(treeNodeType.TransferModes.Length)];
            if (Enum.TryParse<Cluster.Model.DataPortTransferMode>(mode.ToString(), out var parsed))
                return parsed;
        }

        return Cluster.Model.DataPortTransferMode.None;
    }

    private enum GenerateLinksType
    {
        DataPort,
        Hidden,
        Visible
    }
}
#endif
