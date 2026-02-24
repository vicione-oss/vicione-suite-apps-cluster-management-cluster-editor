using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ViciOne.Ui.ClusterEditor.Helpers;

internal static partial class Color
{
    private static readonly ConcurrentDictionary<string, double> s_luminanceByColor = new(StringComparer.Ordinal);

    public static double GetRelativeLuminance(string rgbColorString)
        => s_luminanceByColor.GetOrAdd(rgbColorString, static key =>
        {
            var match = RGBStringRegex().Match(key);

            var r = Convert.ToInt32(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var g = Convert.ToInt32(match.Groups[2].Value, CultureInfo.InvariantCulture);
            var b = Convert.ToInt32(match.Groups[3].Value, CultureInfo.InvariantCulture);

            // https://en.wikipedia.org/wiki/Relative_luminance
            return (0.2126 * r / 255) + (0.7152 * g / 255) + (0.0722 * b / 255);
        });

    [GeneratedRegex("rgb\\((\\d{1,3}), (\\d{1,3}), (\\d{1,3})\\)")]
    private static partial Regex RGBStringRegex();
}
