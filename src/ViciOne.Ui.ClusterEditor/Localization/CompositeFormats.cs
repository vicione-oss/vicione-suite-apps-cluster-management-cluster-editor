using System.Globalization;
using System.Text;
using ViciOne.Ui.Localization.Resources;

namespace ViciOne.Ui.ClusterEditor.Localization;

internal static class CompositeFormats
{
    private static readonly CompositeFormat s_collapseSomething = CompositeFormat.Parse(UserActions.CollapseSomething);
    private static readonly CompositeFormat s_deleteSomething = CompositeFormat.Parse(UserActions.DeleteSomething);
    private static readonly CompositeFormat s_editSomething = CompositeFormat.Parse(UserActions.EditSomething);
    private static readonly CompositeFormat s_expandSomething = CompositeFormat.Parse(UserActions.ExpandSomething);
    private static readonly CompositeFormat s_generateSomething = CompositeFormat.Parse(UserActions.GenerateSomething);
    private static readonly CompositeFormat s_loadSomething = CompositeFormat.Parse(UserActions.LoadSomething);
    private static readonly CompositeFormat s_selectSomething = CompositeFormat.Parse(UserActions.SelectSomething);

    public static string CollapseSomething(string something)
        => string.Format(CultureInfo.CurrentCulture, s_collapseSomething, something);

    public static string DeleteSomething(string something)
        => string.Format(CultureInfo.CurrentCulture, s_deleteSomething, something);

    public static string EditSomething(string something)
        => string.Format(CultureInfo.CurrentCulture, s_editSomething, something);

    public static string ExpandSomething(string something)
        => string.Format(CultureInfo.CurrentCulture, s_expandSomething, something);

    public static string GenerateSomething(string something)
        => string.Format(CultureInfo.CurrentCulture, s_generateSomething, something);

    public static string LoadSomething(string something)
        => string.Format(CultureInfo.CurrentCulture, s_loadSomething, something);

    public static string SelectSomething(string something)
        => string.Format(CultureInfo.CurrentCulture, s_selectSomething, something);
}
