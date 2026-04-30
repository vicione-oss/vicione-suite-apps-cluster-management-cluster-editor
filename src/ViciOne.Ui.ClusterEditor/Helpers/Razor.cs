using System;
using System.Globalization;

namespace ViciOne.Ui.ClusterEditor.Helpers;

internal static class Razor
{
    public static string GetCssPx(double pxNumber)
    {
        if (!double.IsFinite(pxNumber))
            throw new ArgumentException("pxNumber has to be finite.");

        return $"{Math.Floor(pxNumber).ToString(CultureInfo.InvariantCulture)}px";
    }

    public static string GetCssPx(int pxNumber)
        => $"{pxNumber.ToString(CultureInfo.InvariantCulture)}px";
}
