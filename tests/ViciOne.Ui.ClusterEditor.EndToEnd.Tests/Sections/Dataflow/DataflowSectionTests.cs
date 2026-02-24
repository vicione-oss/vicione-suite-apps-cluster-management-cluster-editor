using ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Extensions;
using ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Infrastructure;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Sections.Dataflow;

[Collection(TestWebApplicationFactoryCollection.Name)]
public class DataflowSectionTests(TestWebApplicationFactory fixture) : IClassFixture<TestWebApplicationFactory>
{
    private readonly string _serverAddress = fixture.ServerAddress;

    [Fact]
    [Trait(Traits.Category, Traits.EndToEnd)]
    public async Task Section_should_render()
    {
        var browser = new BrowserFixture(new());
        await browser.WithPageAsync(async page =>
        {
            await page.GotoAsync(_serverAddress);

            var locator = await page.ShowSectionAndGetLocator(SectionNames.Dataflow);
            await Expect(locator).ToBeVisibleAsync();
        });
    }
}
