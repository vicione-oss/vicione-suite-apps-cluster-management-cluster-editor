using System;
using ViciOne.Cluster.Builder.Abstractions;

namespace ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Models;

public sealed class ContainerEditorChildContainer(IContainerEditor editor)
{
    public string? BackColor
    {
        get => Backup.Container.BackColor;
        set
        {
            if (value == Backup.Container.BackColor)
                return;
            editor.SetBackColor(Backup.Container, value ?? null);
            Changed = true;
        }
    }
    public required ContainerEditorChildContainerBackup Backup { get; init; }
    public bool Changed { get; private set; }
    public string? Description
    {
        get => Backup.Container.Description;
        set
        {
            if (value == Backup.Container.Description)
                return;
            editor.SetDescription(Backup.Container, value ?? null);
            Changed = true;
        }
    }
    public string? ForeColor
    {
        get => Backup.Container.ForeColor;
        set
        {
            if (value == Backup.Container.ForeColor)
                return;
            editor.SetForeColor(Backup.Container, value ?? null);
            Changed = true;
        }
    }
    public string Name
    {
        get => Backup.Container.Name;
        set
        {
            if (value == Backup.Container.Name)
                return;

            try
            {
                editor.SetName(Backup.Container, value);
            }
            catch (ArgumentException)
            {
                return;
            }

            Changed = true;
        }
    }
}
