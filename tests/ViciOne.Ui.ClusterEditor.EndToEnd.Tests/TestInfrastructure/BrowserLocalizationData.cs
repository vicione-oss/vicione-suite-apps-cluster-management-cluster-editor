namespace ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Infrastructure;

internal sealed class BrowserLocalizationData
{
    /// <summary>
    /// Locale will affect navigator.language value, Accept-Language request header value as
    /// well as number and date formatting rules
    /// </summary>
    public string Locale { get; set; } = "";

    /// <summary>
    /// See https://en.wikipedia.org/wiki/List_of_tz_database_time_zones
    /// </summary>
    public string TimezoneId { get; set; } = "";
}
