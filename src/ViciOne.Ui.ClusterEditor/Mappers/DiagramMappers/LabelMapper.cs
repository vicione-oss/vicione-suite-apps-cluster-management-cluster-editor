using Blazor.Diagrams.Core.Geometry;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;

namespace ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;

internal static class LabelMapper
{
    internal static LabelNode CreateNode(Label label)
    {
        var node = new LabelNode(new(label.X, label.Y))
        {
            BackgroundColor = label.BackColor ?? string.Empty,
            BorderColor = label.BorderColor ?? string.Empty,
            Height = label.Height,
            Order = label.ZIndex,
            Size = new(label.Width, label.Height),
            Text = label.Content ?? string.Empty,
            Width = label.Width
        };

        return node;
    }

    internal static void PropertyChanged(Label label, LabelNode labelNode, string propertyName)
    {
        switch (propertyName)
        {
            case nameof(Label.BackColor):
                labelNode.BackgroundColor = label.BackColor ?? string.Empty;
                break;
            case nameof(Label.BorderColor):
                labelNode.BorderColor = label.BorderColor ?? string.Empty;
                break;
            case nameof(Label.Content):
                labelNode.Text = label.Content ?? string.Empty;
                break;
            case nameof(Label.Height):
                labelNode.Height = label.Height;
                labelNode.Size = new Size(labelNode.Size!.Width, label.Height);
                break;
            case nameof(Label.Width):
                labelNode.Width = label.Width;
                labelNode.Size = new Size(label.Width, labelNode.Size!.Height);
                break;
            case nameof(Label.X):
            case nameof(Label.Y):
                labelNode.SetPosition(label.X, label.Y);
                break;
            case nameof(Label.ZIndex):
                labelNode.Order = label.ZIndex;
                break;
        }

        labelNode.Refresh();
    }

    internal static void UpdatePosition(IDatastoreState datastoreState, LabelNode labelNode)
    {
        var label = datastoreState.DataflowDiagramMapping.GetModel(labelNode);
        datastoreState.Builder.Editors.Label.SetLocation(
            label,
            new((int)labelNode.Position.X, (int)labelNode.Position.Y)
        );
    }
}
