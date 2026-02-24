using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Playwright;

namespace ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Infrastructure;

internal sealed class BrowserFixture(BrowserFixtureOptions options)
{
    private async Task<IBrowser> CreateBrowserAsync(IPlaywright playwright)
    {
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Channel = options.BrowserChannel
        };

        if (Debugger.IsAttached)
        {
            launchOptions.Headless = false;
            launchOptions.SlowMo = 1000;
        }

        var browserType = playwright[options.BrowserType];

        return await browserType.LaunchAsync(launchOptions);
    }

    /// <summary>
    /// Creating a new browser context makes sure cookies/cache won't be shared with other tests
    /// It's basically a new Incognito window
    /// </summary>
    private async Task<IBrowserContext> CreateBrowserContextAsync(IBrowser browser)
    {
        var contextOptions = new BrowserNewContextOptions
        {
            Locale = options.BrowserLocalization.Locale,
            TimezoneId = options.BrowserLocalization.TimezoneId,
        };

        return await browser.NewContextAsync(contextOptions);
    }

    private string GenerateFileName(string testName, string extension)
    {
        var browserType = options.BrowserType;

        if (!string.IsNullOrEmpty(options.BrowserChannel))
        {
            browserType += "_" + options.BrowserChannel;
        }

        var os = OperatingSystem.IsLinux() ? "linux" :
            OperatingSystem.IsMacOS() ? "macos" :
            OperatingSystem.IsWindows() ? "windows" :
            "other";

        var utcNow = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
        return $"{testName}_{browserType}_{os}_{utcNow}{extension}";
    }

    public async Task TryCaptureScreenshotAsync(IPage page, string testName)
    {
        try
        {
            var fileName = GenerateFileName(testName, ".png");
            var path = Path.Combine("screenshots", fileName);

            await page.ScreenshotAsync(new() { Path = path });

            Console.WriteLine($"Screenshot saved to {path}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to capture screenshot: " + ex);
        }
    }

    public async Task WithPageAsync(Func<IPage, Task> action, [CallerMemberName] string testName = "")
    {
        using var playwright = await Playwright.CreateAsync();

        await using (var browser = await CreateBrowserAsync(playwright))
        {
            await using var ctx = await CreateBrowserContextAsync(browser);

            var page = await ctx.NewPageAsync();

            page.Console += (_, e) => Console.WriteLine(e.Text);
            page.PageError += (_, e) => Console.WriteLine(e);

            try
            {
                await action(page);
            }
            catch
            {
                if (options.CaptureScreenshotOnError)
                    await TryCaptureScreenshotAsync(page, testName);

                throw;
            }
        }
    }
}
