using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ViciOne.Ui.ClusterEditor.Helpers;

internal static partial class Color
{
    private const string CssColorComponentPattern = @"[+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:e[+-]?\d+)?(?:%|deg|rad|grad|turn)?";

    /// <summary>
    /// Named colors of CSS Color Module Level 4 plus <c>transparent</c>.
    /// </summary>
    private static readonly FrozenSet<string> s_cssNamedColors = FrozenSet.ToFrozenSet(
    [
        "aliceblue", "antiquewhite", "aqua", "aquamarine", "azure", "beige", "bisque", "black", "blanchedalmond",
        "blue", "blueviolet", "brown", "burlywood", "cadetblue", "chartreuse", "chocolate", "coral", "cornflowerblue",
        "cornsilk", "crimson", "cyan", "darkblue", "darkcyan", "darkgoldenrod", "darkgray", "darkgreen", "darkgrey",
        "darkkhaki", "darkmagenta", "darkolivegreen", "darkorange", "darkorchid", "darkred", "darksalmon",
        "darkseagreen", "darkslateblue", "darkslategray", "darkslategrey", "darkturquoise", "darkviolet", "deeppink",
        "deepskyblue", "dimgray", "dimgrey", "dodgerblue", "firebrick", "floralwhite", "forestgreen", "fuchsia",
        "gainsboro", "ghostwhite", "gold", "goldenrod", "gray", "green", "greenyellow", "grey", "honeydew", "hotpink",
        "indianred", "indigo", "ivory", "khaki", "lavender", "lavenderblush", "lawngreen", "lemonchiffon", "lightblue",
        "lightcoral", "lightcyan", "lightgoldenrodyellow", "lightgray", "lightgreen", "lightgrey", "lightpink",
        "lightsalmon", "lightseagreen", "lightskyblue", "lightslategray", "lightslategrey", "lightsteelblue",
        "lightyellow", "lime", "limegreen", "linen", "magenta", "maroon", "mediumaquamarine", "mediumblue",
        "mediumorchid", "mediumpurple", "mediumseagreen", "mediumslateblue", "mediumspringgreen", "mediumturquoise",
        "mediumvioletred", "midnightblue", "mintcream", "mistyrose", "moccasin", "navajowhite", "navy", "oldlace",
        "olive", "olivedrab", "orange", "orangered", "orchid", "palegoldenrod", "palegreen", "paleturquoise",
        "palevioletred", "papayawhip", "peachpuff", "peru", "pink", "plum", "powderblue", "purple", "rebeccapurple",
        "red", "rosybrown", "royalblue", "saddlebrown", "salmon", "sandybrown", "seagreen", "seashell", "sienna",
        "silver", "skyblue", "slateblue", "slategray", "slategrey", "snow", "springgreen", "steelblue", "tan", "teal",
        "thistle", "tomato", "transparent", "turquoise", "violet", "wheat", "white", "whitesmoke", "yellow",
        "yellowgreen"
    ], StringComparer.OrdinalIgnoreCase);

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

    /// <summary>
    /// Checks that <paramref name="value"/> is a hex, <c>rgb()</c>/<c>rgba()</c>, <c>hsl()</c>/<c>hsla()</c> or named
    /// CSS color. Anything else could render no color or terminate the CSS declaration it is rendered into.
    /// </summary>
    public static bool IsValidCssColor(string? value)
    {
        if (value is null)
            return false;

        var trimmed = value.Trim();

        return s_cssNamedColors.Contains(trimmed)
            || HexCssColorRegex().IsMatch(trimmed)
            || FunctionalCssColorRegex().IsMatch(trimmed);
    }

    public static string SanitizeCssColor(string? value, string fallback)
        => IsValidCssColor(value) ? value! : fallback;

    [GeneratedRegex("rgb\\((\\d{1,3}), (\\d{1,3}), (\\d{1,3})\\)")]
    private static partial Regex RGBStringRegex();

    [GeneratedRegex("^#(?:[0-9a-f]{3,4}|[0-9a-f]{6}|[0-9a-f]{8})$", RegexOptions.IgnoreCase)]
    private static partial Regex HexCssColorRegex();

    /// <summary>
    /// Matches the legacy comma separated and the modern space separated syntax, e.g. <c>rgb(1, 2, 3)</c> or
    /// <c>hsl(120deg 50% 50% / 0.5)</c>.
    /// </summary>
    [GeneratedRegex(
        @"^(?:rgba?|hsla?)\(\s*(?:" +
        CssColorComponentPattern + @"(?:\s*,\s*" + CssColorComponentPattern + @"){2,3}" +
        "|" +
        CssColorComponentPattern + @"(?:\s+" + CssColorComponentPattern + @"){2}(?:\s*/\s*" + CssColorComponentPattern + ")?" +
        @")\s*\)$",
        RegexOptions.IgnoreCase)]
    private static partial Regex FunctionalCssColorRegex();
}
