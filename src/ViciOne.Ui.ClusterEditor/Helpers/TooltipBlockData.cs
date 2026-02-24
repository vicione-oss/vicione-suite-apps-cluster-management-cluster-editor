using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.Localization.Resources;
using LocalTechnicalTerms = ViciOne.Ui.ClusterEditor.Localization.Resources.TechnicalTerms;

namespace ViciOne.Ui.ClusterEditor.Helpers;

internal static class TooltipBlockData
{
    private static List<List<string>?> GetBlockTooltipContent(Block block)
    {
        var content = new List<List<string>?>();

        if (!string.IsNullOrWhiteSpace(block.Description))
            content.Add([CommonVocabulary.Description, block.Description]);

        if (block.IsFunctionBlock)
        {
            if (!string.IsNullOrWhiteSpace(block.Description))
                content.Add(null);

            if (block.FunctionBlockDesign!.RuntimeDriverTypeName is not null)
                content.Add([Localization.TooltipData.BelongsToDriver, block.FunctionBlockDesign.RuntimeDriverTypeName]);

            content.Add([LocalTechnicalTerms.Namespace, block.FunctionBlockDesign.Namespace]);
            content.Add([CommonVocabulary.Design, block.FunctionBlockDesign.Name]);
            content.Add(null);

            content.Add([TechnicalTerms.Id, block.FunctionBlock!.Id.ToString()]);
            content.Add([Localization.TooltipData.AssignedEngine, block.FunctionBlock.Engine?.Name ?? "None"]);
            content.Add(null);

            content.Add([Localization.TooltipData.DefaultRunMode, block.FunctionBlockDesign.DefaultRunMode.ToString()]);
            content.Add([Localization.TooltipData.CurrentRunMode, block.FunctionBlock.RunMode.ToString()]);

            if (block.FunctionBlock.RunMode == FunctionBlockRunMode.Cyclic)
            {
                content.Add([Localization.TooltipData.DefaultCycleFrequency, block.FunctionBlockDesign.DefaultCycleFrequency.ToString(CultureInfo.InvariantCulture)]);
                content.Add([Localization.TooltipData.CurrentCycleFrequency, block.FunctionBlock.CycleFrequency.ToString(CultureInfo.InvariantCulture)]);
            }
        }

        return content;
    }

    public static TooltipInfo GetBlockTooltipInfo(MouseEventArgs e, Block block, BlockNode node, Rectangle parentBounds)
    {
        var content = GetBlockTooltipContent(block);

        var info = new TooltipInfo()
        {
            Header = node.Name,
            ParentBounds = parentBounds,
            PosX = Convert.ToInt32(e.ClientX),
            PosY = Convert.ToInt32(e.ClientY),
            Type = TooltipInfoType.Table
        };
        info.Content.AddRange(content);

        return info;
    }
}
