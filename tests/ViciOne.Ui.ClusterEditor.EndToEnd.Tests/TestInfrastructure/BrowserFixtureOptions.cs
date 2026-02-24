namespace ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Infrastructure;

internal sealed class BrowserFixtureOptions
{
    /// <summary>
    /// It is possible to use the branded Chromium variants Chrome and MSEdge
    /// See https://playwright.dev/dotnet/docs/browsers#chromium
    /// </summary>
    public string? BrowserChannel { get; set; }

    public BrowserLocalizationData BrowserLocalization { get; set; } = BrowserLocalizations.En;

    public string BrowserType { get; set; } = Microsoft.Playwright.BrowserType.Chromium;

    public bool CaptureScreenshotOnError { get; set; }
}
