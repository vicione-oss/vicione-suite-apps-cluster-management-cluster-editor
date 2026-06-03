#if DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.Blazor.Components.ComboBox;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Localization;
using ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.Localization.Resources;
using TechnicalTerms = ViciOne.Ui.ClusterEditor.Localization.Resources.TechnicalTerms;

namespace ViciOne.Ui.ClusterEditor.Sections.Debugging.Components;

public sealed partial class DebugSectionContent : ComponentBase
{
    private bool _debugPopupVisible;
    private readonly string _generateAllFbsText = CompositeFormats.GenerateSomething($"{CommonVocabulary.All} {TechnicalTerms.FunctionBlockPlural}");
    private int _generateBlocksAmount = 25;
    private string _generateBlocksName = "TwoWaySelector";
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

    private async Task GenerateBlocksAsync(IEnumerable<Guid> uniqueIds, int amount)
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
    private bool GenerateDataPortLink()
    {
        var cache = Datastore.Builder.Cache;
        var dataPortTreeNodes = cache.DataPortTreeNodes.Where(n => n.ValueType is not null).ToList();
        if (dataPortTreeNodes.Count == 0)
            return false;
        var dataPortTreeNode = dataPortTreeNodes[_rnd.Next(dataPortTreeNodes.Count)];

        var validConnectors = cache.Connectors
            .Where(c => c.Type != ConnectorType.Setting)
            .Where(c => Datastore.Builder.Editors.DataPortTreeNode.CanAssignConnector(dataPortTreeNode, c))
            .ToList();

        if (validConnectors.Count == 0)
            return false;
        var connector = validConnectors[_rnd.Next(validConnectors.Count)];

        Datastore.Builder.Editors.DataPortTreeNode.AssignConnector(dataPortTreeNode, connector);

        return true;
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is only debug data")]
    private bool GenerateHiddenLink()
    {
        var connectorEditor = Datastore.Builder.Editors.Connector;
        var allConnectors = Datastore.Builder.Cache.Connectors.Where(c => c.Type != ConnectorType.Setting).ToList();

        var outputConnectors = allConnectors.OfType<IConnectorOutput>().ToList();
        if (outputConnectors.Count == 0)
            return false;
        var sourceConnector = outputConnectors[_rnd.Next(outputConnectors.Count)];

        var inputConnectors = allConnectors.OfType<IConnectorInput>().ToList();
        if (inputConnectors.Count == 0)
            return false;

        var validDestinationConnectors = inputConnectors
            .Where(c => connectorEditor.CanCreateLink(sourceConnector, c, false))
            .ToList();
        if (validDestinationConnectors.Count == 0)
            return false;
        var destinationConnector = validDestinationConnectors[_rnd.Next(validDestinationConnectors.Count)];

        connectorEditor.SetPublished(_rnd.NextDouble() < 0.5 ? sourceConnector : destinationConnector, true);
        var link = connectorEditor.AddLink(sourceConnector, destinationConnector, false);

        return true;
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is only debug data")]
    private bool GenerateVisibleLink()
    {
        var connectorEditor = Datastore.Builder.Editors.Connector;
        var visibleBlocks = Datastore.ActiveContainer.FunctionBlocks;
        var visibleContainers = Datastore.ActiveContainer.Containers;

        var outputConnectors = visibleBlocks.SelectMany(b => b.Outputs)
            .Union(visibleContainers.SelectMany(c => c.GetConnectors().OfType<IConnectorOutput>())).ToList();
        if (outputConnectors.Count == 0)
            return false;
        var sourceConnector = outputConnectors[_rnd.Next(outputConnectors.Count)];

        var inputConnectors = visibleBlocks.SelectMany(b => b.Inputs)
            .Union(visibleContainers.SelectMany(c => c.GetConnectors().OfType<IConnectorInput>())).ToList();
        var validDestinationConnectors = inputConnectors.Where(c => connectorEditor.CanCreateLink(sourceConnector, c, true)).ToList();

        if (validDestinationConnectors.Count == 0)
            return false;
        var destinationConnector = validDestinationConnectors[_rnd.Next(validDestinationConnectors.Count)];

        var link = connectorEditor.AddLink(sourceConnector, destinationConnector);
        var nodeLink = LinkMapper.CreateLink(Datastore, link);
        Datastore.DataflowDiagramMapping.Add(link, nodeLink);
        DiagramService.Diagram.Links.Add(nodeLink);

        return true;
    }

    private async Task OnGenerateAllLibraryBlocksClickAsync()
        => await GenerateBlocksAsync(Datastore.Builder.GetFunctionBlockDesigns().Select(d => d.Id), 1);

    private async Task OnGenerateBlocksClickAsync()
    {
        var design = Datastore.Builder.GetFunctionBlockDesigns()
            .FirstOrDefault(d => string.Equals(d.Name, _generateBlocksName, StringComparison.OrdinalIgnoreCase));
        if (design is null)
            return;

        await GenerateBlocksAsync([design.Id], _generateBlocksAmount);
    }

    private void OnGenerateLinksClick(GenerateLinksType type)
    {
        if (_generateLinksAmount < 1)
            return;

        var targetAmount = _generateLinksAmount;
        var currentAmount = 0;
        var availableTries = 20;

        while (currentAmount < targetAmount && availableTries > 0)
        {
            switch (type)
            {
                case GenerateLinksType.Visible:
                    if (GenerateVisibleLink())
                        currentAmount++;
                    else
                        availableTries--;
                    break;
                case GenerateLinksType.Hidden:
                    if (GenerateHiddenLink())
                        currentAmount++;
                    else
                        availableTries--;
                    break;
                case GenerateLinksType.DataPort:
                    if (GenerateDataPortLink())
                        currentAmount++;
                    else
                        availableTries--;
                    break;
            }
        }
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

    private enum GenerateLinksType
    {
        DataPort,
        Hidden,
        Visible
    }
}
#endif
