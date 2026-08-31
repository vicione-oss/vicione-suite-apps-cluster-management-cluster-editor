using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Helpers;

internal static class TooltipSimpleData
{
    public static TooltipInfo GetSimpleTooltipInfo(MouseEventArgs e, string tooltipText, Rectangle parentBounds)
    {
        var info = new TooltipInfo()
        {
            ParentBounds = parentBounds,
            PosX = Convert.ToInt32(e.ClientX),
            PosY = Convert.ToInt32(e.ClientY),
            Type = TooltipInfoType.Line
        };
        info.Content.AddRange([tooltipText]);

        return info;
    }
}
