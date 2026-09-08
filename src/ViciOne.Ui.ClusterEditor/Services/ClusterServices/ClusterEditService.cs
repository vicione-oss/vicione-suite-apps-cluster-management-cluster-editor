using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blazor.Diagrams.Core.Geometry;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Services.ClusterServices;

/// <summary>
/// Owns the create/remove mutations against the active container (function blocks,
/// child containers, labels, links and dataflows). Detaches the builder-event
/// projection around its own diagram changes to avoid duplicate processing.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class ClusterEditService(
    BuilderEventProjectionService builderEvents,
    DiagramEventService diagramEventService,
    DiagramProjectionService projection,
    DatastoreState state)
{
    public async Task<ChildContainerNode> AddChildContainer(DiagramService diagramService, Point position, CancellationToken cancellationToken = default, params IContainerChild[] children)
    {
        builderEvents.Detach();

        var containerEditor = state.Builder.Editors.Container;
        var container = containerEditor.AddContainer(
            state.ActiveContainer,
            children: children,
            location: new((int)position.X, (int)position.Y)
        );
        containerEditor.SetBackColor(container, BlockNodeColors.BackgroundDefault);
        containerEditor.SetForeColor(container, BlockNodeColors.ForegroundDefault);

        var containerNodes = await projection.AddChildContainersToMapping([container], diagramService, cancellationToken);

        builderEvents.Attach();
        diagramEventService.InvokeContainerAdded(container);

        return containerNodes[0];
    }

    public void AddDataflow()
        => state.Builder.Editors.Cluster.AddDataflow("Dataflow", state.Builder.Cluster.Version);

    public async Task<FunctionBlockNode> AddFunctionBlock(DiagramService diagramService, Guid designId, Point position, CancellationToken cancellationToken = default)
    {
        builderEvents.Detach();

        var functionBlock = state.Builder.Editors.Container.AddFunctionBlock(
            state.ActiveContainer,
            designId,
            location: new((int)position.X, (int)position.Y));

        var functionBlockEditor = state.Builder.Editors.FunctionBlock;
        functionBlockEditor.SetForeColor(functionBlock, BlockNodeColors.ForegroundDefault);
        functionBlockEditor.SetBackColor(functionBlock, BlockNodeColors.BackgroundDefault);

        var engine = state.ValidDataflowEngines.FirstOrDefault();
        if (engine is not null)
            functionBlockEditor.AssignEngine(engine, functionBlock);

        var functionBlockNodes = await projection.AddFunctionBlocksToMapping([functionBlock], diagramService, cancellationToken);

        builderEvents.Attach();

        return functionBlockNodes[0];
    }

    public LabelNode AddLabel(Point position, int zIndex)
    {
        builderEvents.Detach();

        var label = state.Builder.Editors.Container.AddLabel(
            state.ActiveContainer,
            new((int)position.X, (int)position.Y),
            new(LabelDefaults.Width, LabelDefaults.Height),
            zIndex,
            LabelDefaults.Content);

        var labelEditor = state.Builder.Editors.Label;
        labelEditor.SetBackColor(label, LabelColors.BackgroundDefault);
        labelEditor.SetBorderColor(label, LabelColors.BorderDefault);

        var labels = projection.AddLabelsToMapping([label]);

        builderEvents.Attach();

        return labels[0];
    }

    public bool AddLink(BlockNodeLink nodeLink)
    {
        var sourceNodeConnector = (BlockNodeConnector)nodeLink.SourcePort!;
        var targetNodeConnector = (BlockNodeConnector)nodeLink.TargetPort!;
        var sourceConnector = (IConnectorOutput)state.DataflowDiagramMapping.GetModel(sourceNodeConnector);
        var targetConnector = (IConnectorInput)state.DataflowDiagramMapping.GetModel(targetNodeConnector);

        if (!state.Builder.Editors.Connector.CanCreateLink(sourceConnector, targetConnector))
            return false;

        var link = state.Builder.Editors.Connector.AddLink(sourceConnector, targetConnector);
        state.DataflowDiagramMapping.Add(link, nodeLink);

        return true;
    }

    public void Remove(ChildContainerNode containerNode)
    {
        var container = state.DataflowDiagramMapping.GetModel(containerNode);

        state.Builder.Editors.Container.RemoveContainer(container);
        state.DataflowDiagramMapping.Remove(container);

        diagramEventService.InvokeContainerRemoved(container);
    }

    public void Remove(FunctionBlockNode functionBlockNode)
    {
        var functionBlock = state.DataflowDiagramMapping.GetModel(functionBlockNode);

        state.Builder.Editors.Container.RemoveFunctionBlock(functionBlock);
        state.DataflowDiagramMapping.Remove(functionBlock);

        diagramEventService.InvokeFunctionBlockRemoved();
    }

    public void Remove(BlockNodeLink nodeLink)
    {
        var link = state.DataflowDiagramMapping.GetModel(nodeLink);

        state.Builder.Editors.Connector.RemoveLink(link);
        state.DataflowDiagramMapping.Remove(link);
    }

    public void Remove(LabelNode labelNode)
    {
        var label = state.DataflowDiagramMapping.GetModel(labelNode);

        state.Builder.Editors.Container.RemoveLabel(label);
        state.DataflowDiagramMapping.Remove(label);
    }

    public void RemoveDataflow(Dataflow dataflow)
        => state.Builder.Editors.Cluster.RemoveDataflow(dataflow);

    public void RemoveMapping(BlockNodeConnector blockNodeConnector)
    {
        var connector = state.DataflowDiagramMapping.GetModel(blockNodeConnector);
        state.DataflowDiagramMapping.Remove(connector);
    }

    public void SetDataflowName(Dataflow dataflow, string newName)
        => state.Builder.Editors.Dataflow.SetName(dataflow, newName);
}
