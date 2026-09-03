using System;
using System.Collections.Generic;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Helpers;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Factories;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Models;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.Shared.Dx.Services;

namespace ViciOne.Ui.ClusterEditor.Models.DiagramModels;

public sealed class BlockNodeConnector : PortModel, IDiagramModel, IDisposable, IDragable, IDragTarget
{
    private const double RelativeLuminanceThreshold = 0.6;

    private readonly ComparerService _comparerService;
    private readonly IDatastoreState _datastoreState;
    private readonly DiagramService _diagramService;
    private bool _hasDefaultEventEnabled = true;
    private bool _hasDefaultMarkAsChangedOnlyIfNotEqual = true;
    private bool _hasDefaultPoolingMode = true;
    private bool _hasDefaultValue = true;
    private bool _isEventEnabled;

    public Cluster.Model.IConnector Connector { get; }
    public string? ConnectorContainerStyle { get; private set; }
    public string ContainerMarkerStyle { get; private set; } = "";
    public ConnectorMarker DataPortConnectorMarker { get; set; }
    public string DataPortMarkerStyle { get; private set; } = "";
    public bool HasChangedProperties => !_hasDefaultEventEnabled ||
        !_hasDefaultMarkAsChangedOnlyIfNotEqual || !_hasDefaultPoolingMode || !_hasDefaultValue;
    public bool HasDefaultConfiguration { get; private set; } = true;
    public bool HasUpstreamLinks { get; private set; }
    public bool IsInput { get; }
    public bool IsOnContainer { get; private set; }
    public bool IsSystemConnector { get; init; }
    public bool IsValidDropTarget { get; private set; }
    public BlockNode Node { get; }
    public Cluster.Model.IConnector? ParentContainerConnector { get; private set; }
    public string PoolingMode { get; private set; } = GetPoolingModeSymbol("Internal");
    public string PoolingModeColor { get; private set; } = BlockNodeConnectorColors.PoolingModeDefault;
    public string? PoolingModeStyle { get; private set; }
    public string PortColor { get; private set; } = BlockNodeConnectorColors.PortDefault;
    public string? PortStyle { get; private set; }
    public string PortText { get; private set; } = string.Empty;
    public string PortTextColor { get; private set; } = BlockNodeConnectorColors.PortTextDefault;
    public bool Published => Connector.Published;
    public ConnectorMarker PublishedConnectorMarker { get; set; }
    public bool Selected { get; private set; }
    public string Text { get; set; } = string.Empty;
    public string TextBackgroundColor { get; private set; } = BlockNodeConnectorColors.TextBackgroundDefault;
    public string TextColor { get; private set; } = BlockNodeConnectorColors.TextDefault;
    public string? TextStyle { get; private set; }
    public new bool Visible { get; set; } = true;

    public BlockNodeConnector(
        ComparerService comparerService,
        Cluster.Model.IConnector connector,
        IDatastoreState datastoreState,
        DiagramService diagramService,
        BlockNode node,
        bool isInput) : base(node, isInput ? PortAlignment.Left : PortAlignment.Right)
    {
        _comparerService = comparerService;
        _datastoreState = datastoreState;
        _diagramService = diagramService;

        Connector = connector;
        DataPortConnectorMarker = new(this) { Type = ConnectorMarkerType.DataPort };
        IsInput = isInput;

        node.AddPort(this);
        Changed += OnChanged;

        if (IsInput)
            Locked = true;

        Node = node;
        PublishedConnectorMarker = new(this) { Type = ConnectorMarkerType.Published };

        UpdateConnectorMarker();
    }

    public void CalculateHasDefaultConfiguration()
    {
        var newValue = _hasDefaultEventEnabled &&
            _hasDefaultMarkAsChangedOnlyIfNotEqual &&
            _hasDefaultPoolingMode &&
            _hasDefaultValue &&
            !IsOnContainer &&
            !DataPortConnectorMarker.Visible &&
            !PublishedConnectorMarker.Visible;

        if (newValue == HasDefaultConfiguration)
            return;

        HasDefaultConfiguration = newValue;
        Node.CalculateDisplayAllConnectors();
    }

    public override bool CanAttachTo(ILinkable other)
    {
        if (other is not PortModel port)
            return false;

        // Stammt aus `base.CanAttachTo` und musste separat implementiert werden um das
        // anknüpfen an der selben Node funktional zu machen.
        if (port == this || port.Locked)
            return false;

        if (port is not BlockNodeConnector fbPort)
            return false;

        if (!fbPort.IsInput)
            return false;

        if (!fbPort.Node.Visible)
            return false;

        var sourcePort = (BlockNodeConnector)_diagramService.DraggingLink!.SourcePort!;
        return _datastoreState.Builder.Editors.Connector.CanCreateLink(
            (IConnectorOutput)_datastoreState.DataflowDiagramMapping.GetModel(sourcePort),
            (IConnectorInput)_datastoreState.DataflowDiagramMapping.GetModel(fbPort)
        );
    }

    public void Dispose()
    {
        Changed -= OnChanged;
        GC.SuppressFinalize(this);
    }

    public Rectangle GetDataPortMarkRect()
    {
        var bounds = GetBounds();
        var gridSize = DiagramSettings.DefaultGridSize;
        var left = bounds.Left + (IsInput ? (-1 * (5 + 8) * gridSize) : ((2 + 8) * gridSize));
        var right = left + (5 * gridSize);
        return new Rectangle(left, bounds.Top, right, bounds.Bottom);
    }

    private static string GetPoolingModeSymbol(string modeName)
        => modeName switch
        {
            "AND" => "∧",
            "Average" => "Ø",
            "Concat" => "+",
            "Internal" => "I",
            "Max" => "▲",
            "Min" => "▼",
            "NAND" => "⊼",
            "NOR" => "⊽",
            "OR" => "∨",
            "Queue" => "≡",
            "Sum" => "∑",
            "XNOR" => "⊙",
            "XOR" => "⊕",
            _ => "*"
        };

    public Rectangle GetPublishedMarkRect()
    {
        var bounds = GetBounds();
        var gridSize = DiagramSettings.DefaultGridSize;
        var left = bounds.Left + (IsInput ? (-1 * (5 + 14) * gridSize) : ((2 + 14) * gridSize));
        var right = left + (5 * gridSize);
        return new Rectangle(left, bounds.Top, right, bounds.Bottom);
    }

    public string GetTooltipHeader(bool isContainerMarker = false)
    {
        if (isContainerMarker && _datastoreState.ActiveContainer is ChildContainer parent)
        {
            var conModel = _datastoreState.DataflowDiagramMapping.GetModel(this);
            var parentConnector = parent.GetConnector(conModel.GetUnderlyingConnector());
            return $"{parentConnector!.ShortName} = {conModel.FunctionBlock.Name}.{conModel.Name}";
        }
        else if (Connector is Connector connector)
        {
            return string.Equals(connector.Name, connector.ShortName, StringComparison.OrdinalIgnoreCase)
                ? connector.Name
                : $"{connector.ShortName} = {connector.Name}";
        }
        else if (Connector is ContainerConnector containerConnector)
        {
            return string.Equals(containerConnector.Name, containerConnector.ShortName, StringComparison.OrdinalIgnoreCase)
                ? $"{containerConnector.ShortName} = {containerConnector.Connector.FunctionBlock.Name}.{containerConnector.Connector.Name}"
                : $"{containerConnector.ShortName} ({containerConnector.Name}) = {containerConnector.Connector.FunctionBlock.Name}.{containerConnector.Connector.Name}";
        }
        else
        {
            var conModel = _datastoreState.DataflowDiagramMapping.GetModel(this);
            return string.Equals(conModel.Name, conModel.ShortName, StringComparison.OrdinalIgnoreCase)
                    ? conModel.Name
                    : $"{conModel.ShortName} = {conModel.Name}";
        }
    }

    public bool HasLink()
        => Links.Count > 0;

    void IDragTarget.HighlightAsTarget(bool highlight)
        => SetIsValidDropTarget(highlight);

    private void OnChanged(Model port)
        // TODO: Dies könnte man optimieren, indem man sich an die _diagramState.ChangeDraggingLink()
        // Methode hängt und dann nur die betroffenen Nodes aktualisiert
        => Node.CalculateDisplayAllConnectors();

    private void RebuildStyleCaches()
    {
        var prefix = IsInput ? "input" : "output";

        ConnectorContainerStyle = TextBackgroundColor == BlockNodeConnectorColors.TextBackgroundDefault
            ? null
            : $"--{prefix}-text-background-color:{TextBackgroundColor}";

        TextStyle = TextColor == BlockNodeConnectorColors.TextDefault
            ? null
            : $"--{prefix}-text-color:{TextColor}";

        PoolingModeStyle = !IsInput || PoolingModeColor == BlockNodeConnectorColors.PoolingModeDefault
            ? null
            : $"--input-pooling-mode-color:{PoolingModeColor}";

        var portColorPart = PortColor == BlockNodeConnectorColors.PortDefault
            ? null
            : $"--{prefix}-port-color:{PortColor}";
        var portTextColorPart = PortTextColor == BlockNodeConnectorColors.PortTextDefault
            ? null
            : $"--{prefix}-port-text-color:{PortTextColor}";

        PortStyle = (portColorPart, portTextColorPart) switch
        {
            (null, null) => null,
            (not null, null) => portColorPart,
            (null, not null) => portTextColorPart,
            _ => $"{portColorPart};{portTextColorPart}"
        };

        ContainerMarkerStyle = $"--container-marker-background-color:{PortColor};--container-marker-text-color:{PortTextColor}";
        DataPortMarkerStyle = $"--dataport-marker-background-color:{PortColor};--dataport-marker-text-color:{PortTextColor}";
    }

    private void SetConnectorColors()
    {
        if (Selected)
        {
            PoolingModeColor = BlockNodeConnectorColors.PoolingModeSelected;
            TextBackgroundColor = BlockNodeConnectorColors.TextBackgroundSelected;
            TextColor = BlockNodeConnectorColors.TextSelected;
        }
        else
        {
            PoolingModeColor = BlockNodeConnectorColors.PoolingModeDefault;
            TextBackgroundColor = BlockNodeConnectorColors.TextBackgroundDefault;
            TextColor = BlockNodeConnectorColors.TextDefault;

            if (!_hasDefaultPoolingMode)
            {
                PoolingModeColor = BlockNodeConnectorColors.PoolingModeNonDefault;
                TextBackgroundColor = BlockNodeConnectorColors.TextBackgroundNonDefault;
            }

            if (HasChangedProperties)
            {
                PoolingModeColor = BlockNodeConnectorColors.PoolingModeNonDefault;
                TextBackgroundColor = BlockNodeConnectorColors.TextBackgroundNonDefault;
                TextColor = BlockNodeConnectorColors.TextNonDefault;
            }

            if (IsValidDropTarget)
            {
                var luminance = Color.GetRelativeLuminance(PortColor);
                PoolingModeColor = luminance < RelativeLuminanceThreshold
                    ? BlockNodeConnectorColors.PoolingModeIsValidDropTargetOnDarkBackground
                    : BlockNodeConnectorColors.PoolingModeIsValidDropTarget;
                TextBackgroundColor = PortColor;
                TextColor = luminance < RelativeLuminanceThreshold
                    ? BlockNodeConnectorColors.TextIsValidDropTargetOnDarkBackground
                    : BlockNodeConnectorColors.TextIsValidDropTarget;
            }
        }

        RebuildStyleCaches();
    }

    public void SetEventEnabled(bool eventEnabled, bool defaultEventEnabled)
    {
        if (eventEnabled == _isEventEnabled)
            return;

        _hasDefaultEventEnabled = eventEnabled == defaultEventEnabled;
        _isEventEnabled = eventEnabled;
        SetConnectorColors();
        SetPortText();
        CalculateHasDefaultConfiguration();
    }

    public void SetHasUpstreamLinks(bool hasUpstreamLinks)
        => HasUpstreamLinks = hasUpstreamLinks;

    public void SetIsOnContainer(bool isOnContainer)
    {
        if (isOnContainer == IsOnContainer)
            return;

        IsOnContainer = isOnContainer;
        CalculateHasDefaultConfiguration();
    }

    public void SetIsValidDropTarget(bool isValidDropTarget)
    {
        if (isValidDropTarget == IsValidDropTarget)
            return;

        IsValidDropTarget = isValidDropTarget;
        SetConnectorColors();
        Locked = !(IsValidDropTarget || !IsInput);
    }

    public void SetMarkAsChangedOnlyIfNotEqual(bool markAsChangedOnlyIfNotEqual, bool defaultMarkAsChangedOnlyIfNotEqual)
    {
        _hasDefaultMarkAsChangedOnlyIfNotEqual = markAsChangedOnlyIfNotEqual == defaultMarkAsChangedOnlyIfNotEqual;
        SetConnectorColors();
        CalculateHasDefaultConfiguration();
    }

    public void SetParentContainerConnector(Cluster.Model.IConnector? connector)
        => ParentContainerConnector = connector;

    public void SetPoolingMode(IConnectorInput input)
    {
        var defaultPoolingMode = _datastoreState.Builder.ResolveDefaultCrossSourceAggregatingPooling(input.GetUnderlyingConnector());
        SetPoolingMode(input.CrossSourcePoolingStrategy, input.CrossSourceAggregatingPooling, defaultPoolingMode);
    }

    public void SetPoolingMode(PoolingStrategy poolingStrategy, IAggregatingPooling? poolingMode, IAggregatingPooling? defaultPoolingMode)
    {
        _hasDefaultPoolingMode = _comparerService.GetComparer(typeof(IAggregatingPooling))!
            .Compare(poolingMode, defaultPoolingMode) == 0;

        if (poolingStrategy == PoolingStrategy.Aggregate && poolingMode is not null)
            PoolingMode = GetPoolingModeSymbol(poolingMode.Name);
        else if (poolingStrategy != PoolingStrategy.Aggregate)
            PoolingMode = GetPoolingModeSymbol(poolingStrategy.ToString());

        SetConnectorColors();
        CalculateHasDefaultConfiguration();
    }

    public void SetPortColor(string color)
    {
        if (color == PortColor)
            return;

        PortColor = color;
        PortTextColor = Color.GetRelativeLuminance(color) < RelativeLuminanceThreshold
            ? BlockNodeConnectorColors.PortTextOnDarkBackground
            : BlockNodeConnectorColors.PortTextDefault;

        RebuildStyleCaches();
    }

    private void SetPortText()
    {
        PortText = "";
        if (_isEventEnabled)
            PortText += "E";
    }

    public void SetPublished(bool isPublished)
    {
        if (isPublished == PublishedConnectorMarker.Visible)
            return;

        PublishedConnectorMarker.Visible = isPublished;
        CalculateHasDefaultConfiguration();
    }

    public void SetSelection(bool isSelected)
    {
        if (isSelected == Selected)
            return;

        Selected = isSelected;
        SetConnectorColors();
    }

    public void SetTooltipEntries(IEnumerable<PublishedConnectorTooltipEntry> entries)
        => PublishedConnectorMarker.TooltipEntries = entries;

    public void SetValue(IConnectorInput input)
    {
        if (Node is FunctionBlockNode fbNode)
        {
            if (input.Name == SystemConnectorNames.FunctionBlockEnabledName)
            {
                if (input.Value is bool inputValue)
                    fbNode.Enabled = inputValue;
            }
        }

        var underlyingConnector = input.GetUnderlyingConnector();
        SetValue(underlyingConnector.Value, _datastoreState.Builder.ResolveDefaultValue(underlyingConnector));
    }

    public void SetValue(object? value, object? defaultValue)
    {
        _hasDefaultValue = value == defaultValue || (value?.Equals(defaultValue) ?? false);
        SetConnectorColors();
        CalculateHasDefaultConfiguration();
    }

    public bool ShouldDisplayConnector()
        => !HasDefaultConfiguration ||
           IsValidDropTarget ||
           HasLink() ||
           Selected;

    public void UpdateConnectorMarker()
    {
        DataPortConnectorMarker.Links.Clear();
        PublishedConnectorMarker.Links.Clear();

        foreach (var link in Connector.Links)
        {
            if (link.Visible)
                continue;

            if (link.SourceConnector is not null && link.DestinationConnector is not null)
                PublishedConnectorMarker.Links.Add(link);
            else
                DataPortConnectorMarker.Links.Add(link);
        }

        DataPortConnectorMarker.Visible = DataPortConnectorMarker.Links.Count > 0;
        DataPortConnectorMarker.Selected = DataPortConnectorMarker.Visible && DataPortConnectorMarker.Selected;
        PublishedConnectorMarker.TooltipEntries = PublishedConnectorTooltipEntriesFactory.Create(Connector, _datastoreState.ActiveContainer.Id);
        PublishedConnectorMarker.Visible = PublishedConnectorMarker.Links.Count > 0 || Connector.Published;
        PublishedConnectorMarker.Selected = PublishedConnectorMarker.Visible && PublishedConnectorMarker.Selected;

        CalculateHasDefaultConfiguration();
        Node.Refresh();
    }
}
