using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Models;

namespace ViciOne.Ui.ClusterEditor.Helpers;

internal static class TooltipPublishMarkerData
{
    private static readonly CompositeFormat s_compositeMoreLinks = CompositeFormat.Parse(Localization.TooltipData.MoreLinks);

    private static List<List<string>?> GetPublishMarkerTooltipContent(BlockNodeConnector connector)
    {
        var content = new List<List<string>?>();

        var maxTooltipLines = 20;
        var otherLevelCount = 0;
        var otherLevelMax = 0;
        var otherLevelOverflow = 0;
        var sameLevelCount = 0;
        var sameLevelMax = 0;
        var sameLevelOverflow = 0;
        PublishedConnectorTooltipEntry[] otherLevelEntries;
        PublishedConnectorTooltipEntry[] sameLevelEntries;

        sameLevelEntries = [.. connector.PublishedConnectorMarker.TooltipEntries.Where(te => te.IsSameLevel)];
        otherLevelEntries = [.. connector.PublishedConnectorMarker.TooltipEntries.Where(te => !te.IsSameLevel)];
        var hasSameLevelEntries = sameLevelEntries.Length != 0;
        var hasOtherLevelEntries = otherLevelEntries.Length != 0;
        var showDivider = hasSameLevelEntries && hasOtherLevelEntries;
        var showSameLevelMoreMessage = false;
        var showOtherLevelMoreMessage = false;

        if (showDivider)
        {
            sameLevelMax = sameLevelCount = sameLevelEntries.Length;
            otherLevelMax = otherLevelCount = otherLevelEntries.Length;
            var lineCount = 0;
            if (sameLevelMax + otherLevelMax > maxTooltipLines)
            {
                var sameDiff = sameLevelMax - (maxTooltipLines / 2);
                if (sameDiff <= 0)
                {
                    lineCount = sameLevelCount;
                }
                else
                {
                    sameLevelCount = maxTooltipLines - Math.Min(otherLevelMax, maxTooltipLines / 2) - 1;
                    sameLevelOverflow = sameLevelMax - sameLevelCount;
                    showSameLevelMoreMessage = sameLevelOverflow > 0;
                    lineCount = sameLevelCount + 1;
                }

                var leftLineCount = maxTooltipLines - lineCount;
                if (otherLevelMax > leftLineCount)
                {
                    otherLevelCount = leftLineCount - 1;
                    otherLevelOverflow = otherLevelMax - leftLineCount + 1;
                    showOtherLevelMoreMessage = true;
                }
            }
        }
        else
        {
            if (hasSameLevelEntries)
            {
                sameLevelMax = sameLevelCount = sameLevelEntries.Length;
                if (sameLevelMax > maxTooltipLines)
                {
                    sameLevelCount = maxTooltipLines - 1;
                    showSameLevelMoreMessage = true;
                    sameLevelOverflow = sameLevelMax - sameLevelCount;
                }
            }

            if (hasOtherLevelEntries)
            {
                otherLevelMax = otherLevelCount = otherLevelEntries.Length;
                if (otherLevelMax > maxTooltipLines)
                {
                    otherLevelCount = maxTooltipLines - 1;
                    showOtherLevelMoreMessage = true;
                    otherLevelOverflow = otherLevelMax - otherLevelCount;
                }
            }
        }

        for (var i = 0; i < sameLevelCount; i++)
        {
            content.Add([sameLevelEntries[i].Path]);
        }

        if (showSameLevelMoreMessage)
        {
            content.Add([string.Format(CultureInfo.InvariantCulture, s_compositeMoreLinks, sameLevelOverflow)]);
        }

        if (showDivider)
        {
            content.Add(null);
        }

        for (var i = 0; i < otherLevelCount; i++)
        {
            content.Add([otherLevelEntries[i].Path]);
        }

        if (showOtherLevelMoreMessage)
        {
            content.Add([string.Format(CultureInfo.InvariantCulture, s_compositeMoreLinks, otherLevelOverflow)]);
        }

        return content;
    }

    public static TooltipInfo GetPublishMarkerTooltipInfo(MouseEventArgs e, BlockNodeConnector connector, Rectangle parentBounds)
    {
        var content = GetPublishMarkerTooltipContent(connector);

        var info = new TooltipInfo()
        {
            Header = connector.GetTooltipHeader(),
            ParentBounds = parentBounds,
            PosX = Convert.ToInt32(e.ClientX),
            PosY = Convert.ToInt32(e.ClientY),
            Type = TooltipInfoType.Line
        };
        info.Content.AddRange(content);

        return info;
    }
}
