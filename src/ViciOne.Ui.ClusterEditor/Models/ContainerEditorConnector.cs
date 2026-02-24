using System;
using ViciOne.Cluster.Builder;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;

namespace ViciOne.Ui.ClusterEditor.Models;

public sealed class ContainerEditorConnector(ConnectorEditor editor)
{
    public required ContainerEditorConnectorBackup Backup { get; init; }
    public BlockNodeConnector? BlockNodeConnector { get; set; }
    public bool Changed { get; private set; }
    public string Color { get; init; } = string.Empty;
    public string? Description
    {
        get => Backup.Connector.Description;
        set
        {
            if (value == Backup.Connector.Description)
                return;

            editor.SetDescription(Backup.Connector, value ?? string.Empty);
            Changed = true;
        }
    }
    public int Index
    {
        get => Convert.ToInt32(Backup.Connector.Index);
        set
        {
            if (value == Backup.Connector.Index || value < 0)
                return;

            editor.SetIndex(Backup.Connector, Convert.ToUInt32(value));
            Changed = true;
        }
    }
    public bool IsPlaceholder { get; set; }
    public string Name
    {
        get => Backup.Connector.Name;
        set
        {
            if (value == Backup.Connector.Name)
                return;

            try
            {
                editor.SetName(Backup.Connector, value);
            }
            catch (ArgumentException)
            {
                return;
            }

            Changed = true;
        }
    }
    public bool Selected { get; set; }
    public string ShortName
    {
        get => Backup.Connector.ShortName;
        set
        {
            if (value == Backup.Connector.ShortName)
                return;

            try
            {
                editor.SetShortName(Backup.Connector, value);
            }
            catch (ArgumentException)
            {
                return;
            }

            Changed = true;
        }
    }
}
