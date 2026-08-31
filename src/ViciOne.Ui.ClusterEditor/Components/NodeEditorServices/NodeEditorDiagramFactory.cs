using System;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Models;
using ViciOne.Ui.ClusterEditor.Components.DiagramComponents;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;

namespace ViciOne.Ui.ClusterEditor.Components.NodeEditorServices;

internal static class NodeEditorDiagramFactory
{
    public static BlazorDiagram Create()
    {
        var diagram = new BlazorDiagram(new()
        {
            AllowMultiSelection = true,
            GridSize = DiagramSettings.DefaultGridSize,
            Links =
            {
                Factory = (diagram, source, targetAnchor) =>
                {
                    if (source is not PortModel sourcePort)
                        throw new InvalidOperationException("Source must be a PortModel");

                    return new BlockNodeLink(new SinglePortAnchor(sourcePort), targetAnchor);
                },
                EnableSnapping = true,
                SnappingRadius = 60
            },
            Virtualization =
            {
                Enabled = false // Might want to change this later when the zoom bug is fixed
            },
            Zoom =
            {
                Inverse = true,
                Maximum = DiagramSettings.ZoomMaximum,
                Minimum = DiagramSettings.ZoomMinimum,
                ScaleFactor = 1.3
            }
        });

        diagram.SetZoom(DiagramSettings.DefaultZoom);

        diagram.RegisterComponent<ChildContainerNode, BlockComponent>();
        diagram.RegisterComponent<LabelNode, LabelComponent>();
        diagram.RegisterComponent<FunctionBlockNode, BlockComponent>();
        diagram.RegisterComponent<BlockNodeLink, BlockLinkComponent>();

        return diagram;
    }
}
