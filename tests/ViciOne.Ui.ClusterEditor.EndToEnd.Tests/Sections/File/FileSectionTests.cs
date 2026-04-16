using Microsoft.Playwright;
using ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Extensions;
using ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Helper;
using ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Infrastructure;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Sections.File;

[Collection<ServerTestCollection>]
public class FileSectionTests(ServerTestCollectionFixture fixture)
{
    [Fact]
    public async Task New_should_clear_diagram()
    {
        var browser = new BrowserFixture(new());
        await browser.WithPageAsync(async page =>
        {
            await page.GotoAsync(fixture.ServerAddress);

            await DiagramHelper.AddFunctionBlockAsync(page, "DecimalToIntegral", 2);
            await DiagramHelper.AddContainerAsync(page, new() { X = 10, Y = 10 });
            await DiagramHelper.AddLabelAsync(page, new() { X = 160, Y = 10 });
            await Expect(page.Locator(".diagram-canvas .diagram-node")).ToHaveCountAsync(4);

            var fileLocator = await page.ShowSectionAndGetLocator(SectionNames.File);
            await fileLocator.GetByRole(AriaRole.Button, new() { Exact = true, Name = "New" }).ClickAsync();

            await Expect(page.Locator(".diagram-canvas .diagram-node")).ToHaveCountAsync(0);
        });
    }

    [Fact]
    public async Task Section_should_render()
    {
        var browser = new BrowserFixture(new());
        await browser.WithPageAsync(async page =>
        {
            await page.GotoAsync(fixture.ServerAddress);

            var locator = await page.ShowSectionAndGetLocator(SectionNames.File);
            await Expect(locator).ToBeVisibleAsync();
        });
    }
}
