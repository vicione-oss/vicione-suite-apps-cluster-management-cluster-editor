using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace Shared.Localization;

internal static class CompositeFormats
{
    private static readonly ConcurrentDictionary<(string Culture, string Format), CompositeFormat> s_formatCache = new();

    /// <summary>
    /// Formats a localized composite format string, caching the parsed <see cref="CompositeFormat"/>
    /// per UI culture so that a culture switch is picked up correctly.
    /// </summary>
    public static string Format(string localizedFormat, params object?[] arguments)
        => string.Format(CultureInfo.CurrentCulture, GetFormat(localizedFormat), arguments);

    private static CompositeFormat GetFormat(string localizedFormat)
        => s_formatCache.GetOrAdd(
            (CultureInfo.CurrentUICulture.Name, localizedFormat),
            static key => CompositeFormat.Parse(key.Format));
}
