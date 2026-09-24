using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Extensions;

internal static class IPageExtension
{
    private static async Task<ILocator> GetDataflowToolbarSectionContentLocatorAsync(this IPage page, SectionNames section)
    {
        var title = section switch
        {
            SectionNames.DataPorts => "DataPorts",
            SectionNames.Library => "Library",
            SectionNames.Property => "Properties",
            SectionNames.PublishedConnectors => "Published Connectors",
            SectionNames.SearchAndTools => "Search & Tools",
            SectionNames.Topology => "Cluster Topology",
            _ => throw new ArgumentOutOfRangeException(nameof(section)),
        };

        var sidebarLocator = page.Locator(".sidebar--right");
        var sectionLocator = sidebarLocator.Locator(".flyout > .section:not( .hidden) .section-layout");
        var sectionContentLocator = sectionLocator.Locator(".content");

        var headerTitleLocator = sectionLocator
            .Locator("> .header > .title")
            .GetByText(title, new() { Exact = true });

        if (!await headerTitleLocator.IsVisibleAsync())
        {
            var buttonLocator = sidebarLocator.GetByRole(AriaRole.Button, new() { Exact = true, Name = title });
            await buttonLocator.ClickAsync();

            await Expect(headerTitleLocator).ToBeVisibleAsync();
        }

        return sectionContentLocator;
    }

    private static async Task<ILocator> GetInfrastructureToolbarSectionContentLocatorAsync(this IPage page, SectionNames section)
    {
        var buttonName = section switch
        {
            SectionNames.Dataflow => "Dataflow",
            SectionNames.File => "File",
            SectionNames.Information => "Information",
            _ => throw new ArgumentOutOfRangeException(nameof(section)),
        };
        var contentContainerClass = section switch
        {
            SectionNames.Dataflow => ".dataflow-section-container",
            SectionNames.File => ".file-section-container",
            SectionNames.Information => ".information-section-container",
            _ => throw new ArgumentOutOfRangeException(nameof(section)),
        };
        var sidebarLocator = page.Locator(".sidebar--left");
        var resultLocator = sidebarLocator
            .Locator(contentContainerClass);

        if (!await resultLocator.IsVisibleAsync())
        {
            var buttonLocator = sidebarLocator.GetByRole(AriaRole.Button, new() { Exact = true, Name = buttonName });
            await buttonLocator.ClickAsync();
        }

        return resultLocator;
    }

    public static async Task<ILocator> ShowSectionAndGetLocator(this IPage page, SectionNames section) => section switch
    {
        SectionNames.Dataflow or SectionNames.File or SectionNames.Information =>
            await GetInfrastructureToolbarSectionContentLocatorAsync(page, section),
        SectionNames.DataPorts or SectionNames.Library or SectionNames.Property or SectionNames.PublishedConnectors or
            SectionNames.SearchAndTools or SectionNames.Topology =>
                await GetDataflowToolbarSectionContentLocatorAsync(page, section),
        _ => throw new ArgumentOutOfRangeException(nameof(section)),
    };
}
