using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using ViciOne.Ui.Localization.Resources;

namespace ViciOne.Ui.ClusterEditor.Localization;

internal static class CompositeFormats
{
    private static readonly ConcurrentDictionary<(string Culture, string Format), CompositeFormat> s_formatCache = new();

    public static string CollapseSomething(string something)
        => Format(UserActions.CollapseSomething, something);

    public static string DeleteSomething(string something)
        => Format(UserActions.DeleteSomething, something);

    public static string EditSomething(string something)
        => Format(UserActions.EditSomething, something);

    public static string ExpandSomething(string something)
        => Format(UserActions.ExpandSomething, something);

    /// <summary>
    /// Formats a localized composite format string, caching the parsed <see cref="CompositeFormat"/>
    /// per UI culture so that a culture switch is picked up correctly.
    /// </summary>
    public static string Format(string localizedFormat, object? something)
        => string.Format(CultureInfo.CurrentCulture, GetFormat(localizedFormat), something);

    public static string GenerateSomething(string something)
        => Format(UserActions.GenerateSomething, something);

    private static CompositeFormat GetFormat(string localizedFormat)
        => s_formatCache.GetOrAdd(
            (CultureInfo.CurrentUICulture.Name, localizedFormat),
            static key => CompositeFormat.Parse(key.Format));

    public static string LoadSomething(string something)
        => Format(UserActions.LoadSomething, something);

    public static string SelectSomething(string something)
        => Format(UserActions.SelectSomething, something);
}
