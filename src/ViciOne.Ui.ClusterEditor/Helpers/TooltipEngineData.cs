using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.Localization.Resources;
using Local = ViciOne.Ui.ClusterEditor.Localization.Resources;

namespace ViciOne.Ui.ClusterEditor.Helpers;

internal static class TooltipEngineData
{
    private static List<List<string>?> GetEngineTooltipContent(BlockNode node, List<Cluster.Model.Engine> engines, bool containsUnassignedBlocks)
    {
        var content = new List<List<string>?>();

        if (engines.Count == 1 && !containsUnassignedBlocks)
        {
            var engine = engines.First();

            if (!string.IsNullOrWhiteSpace(engine.Description))
            {
                content.Add([CommonVocabulary.Description, engine.Description]);
                content.Add(null);
            }
            content.Add([TechnicalTerms.Id, node.EngineDisplayText]);
            content.Add([CommonVocabulary.Name, engine.Name]);
            content.Add(null);
            content.Add([Localization.TooltipData.CycleTimeMin, engine.MinCycleTime.ToString(CultureInfo.InvariantCulture)]);
            content.Add([Localization.TooltipData.CycleTimeMax, engine.MaxCycleTime.ToString(CultureInfo.InvariantCulture)]);
        }
        else if (engines.Count == 0 && containsUnassignedBlocks)
        {
            content.Add([Localization.TooltipData.EngineContainsUnassigned, ""]);
        }
        else
        {
            foreach (var engine in engines)
            {
                content.Add([engine.Name]);
            }

            if (containsUnassignedBlocks)
            {
                content.Add(null);
                content.Add([Localization.TooltipData.EngineContainsUnassigned]);
            }
        }

        return content;
    }

    public static TooltipInfo GetEngineTooltipInfo(MouseEventArgs e, BlockNode node, List<Cluster.Model.Engine> engines, bool containsUnassignedBlocks, Rectangle parentBounds)
    {
        var content = GetEngineTooltipContent(node, engines, containsUnassignedBlocks);

        var info = new TooltipInfo
        {
            Header = engines.Count == 1 ? Local.TechnicalTerms.Engine : Local.TechnicalTerms.EnginePlural,
            ParentBounds = parentBounds,
            PosX = Convert.ToInt32(e.ClientX),
            PosY = Convert.ToInt32(e.ClientY),
            Type = TooltipInfoType.Table
        };
        info.Content.AddRange(content);

        return info;
    }
}
