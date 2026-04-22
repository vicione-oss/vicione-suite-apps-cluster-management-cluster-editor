using ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Extensions;
using ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Infrastructure;
using ViciOne.Ui.Testing.Playwright.Infrastructure;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Sections.Library;

[Collection<ServerTestCollection>]
public class LibrarySectionTests(ServerFixture fixture)
{
    [Fact]
    public async Task Section_should_render()
    {
        var browser = new Browser();
        await browser.LaunchAsync(async page =>
        {
            await page.GotoAsync(fixture.ServerAddress);

            var locator = await page.ShowSectionAndGetLocator(SectionNames.Library);
            await Expect(locator).ToBeVisibleAsync();
        });
    }
}
