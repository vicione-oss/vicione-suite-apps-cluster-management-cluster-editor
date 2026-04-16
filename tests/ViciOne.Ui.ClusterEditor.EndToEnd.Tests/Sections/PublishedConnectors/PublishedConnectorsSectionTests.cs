using ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Extensions;
using ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Infrastructure;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Sections.PublishedConnectors;

[Collection<ServerTestCollection>]
public class DataflowSectionTests(ServerTestCollectionFixture fixture)
{
    [Fact]
    public async Task Section_should_render()
    {
        var browser = new BrowserFixture(new());
        await browser.WithPageAsync(async page =>
        {
            await page.GotoAsync(fixture.ServerAddress);

            var locator = await page.ShowSectionAndGetLocator(SectionNames.PublishedConnectors);
            await Expect(locator).ToBeVisibleAsync();
        });
    }
}
