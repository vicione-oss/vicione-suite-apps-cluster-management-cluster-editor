namespace ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Infrastructure;

internal static class BrowserLocalizations
{
    public static BrowserLocalizationData De { get; } = new() { Locale = "de-DE", TimezoneId = "Europe/Berlin" };
    public static BrowserLocalizationData En { get; } = new() { Locale = "en-US", TimezoneId = "America/New_York" };

}
