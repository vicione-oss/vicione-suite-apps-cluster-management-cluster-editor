using System;
using System.Collections.Generic;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;

namespace ViciOne.Ui.ClusterEditor.Models.DiagramModels;

public class Block
{
    public ChildContainer? ChildContainer { get; init; }
    public string? Description
        => IsChildContainer ? ChildContainer?.Description : IsFunctionBlock ? FunctionBlock?.Description : null;
    public FunctionBlock? FunctionBlock { get; init; }
    public Core.Dataflow.DataModel.FunctionBlockDesign? FunctionBlockDesign { get; init; }
    public bool IsChildContainer
        => ChildContainer is not null;
    public bool IsFunctionBlock
        => FunctionBlock is not null;

    public Block()
    { }

    public Block(IDatastoreState datastore, BlockNode node)
    {
        var nodeModel = datastore.DataflowDiagramMapping.GetModel(node);

        if (nodeModel is ChildContainer childContainer)
        {
            ChildContainer = childContainer;
        }
        else if (nodeModel is FunctionBlock functionBlock)
        {
            FunctionBlock = functionBlock;
            FunctionBlockDesign = datastore.Builder.ResolveFunctionBlockDesign(FunctionBlock.DesignId);
        }
        else
        {
            throw new ArgumentException($"Unknown type of {nameof(INamedContainerChild)}");
        }
    }

    public IEnumerable<Cluster.Model.Engine> GetEngines(out bool containsUnassignedBlocks)
    {
        if (ChildContainer is not null)
        {
            return ChildContainer.GetEngines(out containsUnassignedBlocks);
        }
        else if (FunctionBlock is not null)
        {
            var engines = new List<Cluster.Model.Engine>();
            if (FunctionBlock.Engine is not null)
                engines.Add(FunctionBlock.Engine);

            containsUnassignedBlocks = false;
            return engines;
        }

        containsUnassignedBlocks = false;
        return [];
    }
}
