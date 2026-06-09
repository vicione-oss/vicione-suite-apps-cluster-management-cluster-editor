using System.Collections.Generic;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using ViciOne.Ui.ClusterEditor.Constants;

namespace ViciOne.Ui.ClusterEditor.Models.DiagramModels;

public abstract class BlockNode : NodeModel, IDiagramModel
{
    private List<BlockNodeConnector>? _connectorsListCache;

    internal List<BlockNodeConnector?[]> Connectors { get; } = [];
    internal bool DisplayAllConnectors { get; private set; } = true;
    internal string EngineBackgroundColor { get; set; } = BlockNodeColors.EngineBackgroundEmpty;
    internal string EngineDisplayText { get; set; } = Helpers.EngineDisplayText.EngineTextNone;
    internal string? ImageSrc { get; init; }
    internal string ImageText { get; init; } = string.Empty;
    internal string Name { get; set; } = string.Empty;
    internal string NameBackgroundColor { get; set; } = string.Empty;
    internal int NameFieldHeight { get; set; }
    internal string NameForeColor { get; set; } = string.Empty;
    public new bool Visible { get; set; } = true;

    internal BlockNode(Point? point = null) : base(point)
        => ControlledSize = true;

    internal void CalculateDisplayAllConnectors()
    {
        var connectors = ConnectorsToList();
        foreach (var entry in connectors)
        {
            if (!entry.HasDefaultConfiguration || entry.HasLink())
            {
                DisplayAllConnectors = false;
                return;
            }
        }
        DisplayAllConnectors = true;
    }

    private void CalculatePortsPosition()
    {
        for (var i = 0; i < Connectors.Count; i++)
        {
            var input = Connectors[i][0];
            var output = Connectors[i][1];

            if (input is not null)
            {
                input.Size = new Size(DiagramSettings.PortWidth, DiagramSettings.PortHeight);
                input.Position = new Point(Position.X,
                    Position.Y + NameFieldHeight + BlockNodeLayout.SettingsRowHeight + (i * BlockNodeLayout.RowHeight));
                input.Initialized = true;
            }

            if (output is not null)
            {
                output.Size = new Size(DiagramSettings.PortWidth, DiagramSettings.PortHeight);
                output.Position = new Point(Position.X + BlockNodeLayout.Width - DiagramSettings.PortWidth,
                    Position.Y + NameFieldHeight + BlockNodeLayout.SettingsRowHeight + (i * BlockNodeLayout.RowHeight));
                output.Initialized = true;
            }
        }
    }

    internal List<BlockNodeConnector> ConnectorsToList()
    {
        if (_connectorsListCache is null)
        {
            _connectorsListCache = new List<BlockNodeConnector>(Connectors.Count * 2);
            foreach (var row in Connectors)
            {
                foreach (var conn in row)
                {
                    if (conn is not null)
                        _connectorsListCache.Add(conn);
                }
            }
        }

        return _connectorsListCache;
    }

    internal Rectangle GetAlignmentRect()
    {
        var left = Position.X - (DiagramSettings.DefaultGridSize * DiagramSettings.FlagMarginGridCells);
        var top = Position.Y - DiagramSettings.DefaultGridSize;
        var right = Position.X + Size!.Width + (DiagramSettings.DefaultGridSize * DiagramSettings.FlagMarginGridCells);
        var bottom = Position.Y + Size!.Height + DiagramSettings.DefaultGridSize;

        return new(left, top, right, bottom);
    }

    internal BlockNodeConnector? GetDataConnector(uint index, bool isInput)
    {
        var colIndex = isInput ? 0 : 1;
        var rowIndex = BlockNodeLayout.SystemConnectorRows + (int)index;
        return Connectors[rowIndex][colIndex];
    }

    internal BlockNodeConnector? GetSystemConnector(uint index, bool isInput)
    {
        var colIndex = isInput ? 0 : 1;
        var rowIndex = (int)index;
        return Connectors[rowIndex][colIndex];
    }

    internal void InvalidateConnectorsCache()
        => _connectorsListCache = null;

    internal void SetEngineDisplayText(string displayText)
    {
        if (displayText == EngineDisplayText)
            return;

        EngineDisplayText = displayText;
        EngineBackgroundColor = EngineDisplayText switch
        {
            Helpers.EngineDisplayText.EngineTextNone => BlockNodeColors.EngineBackgroundEmpty,
            Helpers.EngineDisplayText.EngineTextMultiple => BlockNodeColors.EngineBackgroundMultiple,
            _ => BlockNodeColors.EngineBackgroundDefault
        };
    }

    internal void UpdateSize()
    {
        Size = new(BlockNodeLayout.Width,
            NameFieldHeight + BlockNodeLayout.SettingsRowHeight + (Connectors.Count * BlockNodeLayout.RowHeight));
        CalculatePortsPosition();
    }
}
