using Microsoft.Playwright;

namespace ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Extensions;

internal static class IPageExtension
{
    private static async Task<ILocator> GetDataflowToolbarSectionContentLocatorAsync(this IPage page, SectionNames section)
    {
        var buttonTitle = section switch
        {
            SectionNames.DataPorts => "DataPorts",
            SectionNames.Library => "Library",
            SectionNames.Property => "Properties",
            SectionNames.PublishedConnectors => "Published Connectors",
            SectionNames.SearchAndTools => "Search & Tools",
            SectionNames.Topology => "Cluster Topology",
            _ => throw new ArgumentOutOfRangeException(nameof(section)),
        };
        var contentContainerClass = section switch
        {
            SectionNames.DataPorts => ".dataport-section-container",
            SectionNames.Library => ".library-section-container",
            SectionNames.Property => ".property-section-container",
            SectionNames.PublishedConnectors => ".published-connectors-section-container",
            SectionNames.SearchAndTools => ".search-and-tools-section-container",
            SectionNames.Topology => ".topology-section-container",
            _ => throw new ArgumentOutOfRangeException(nameof(section)),
        };
        var sidebarLocator = page.Locator(".sidebar--right");
        var resultLocator = sidebarLocator
            .Locator(contentContainerClass);

        if (!await resultLocator.IsVisibleAsync())
        {
            var buttonLocator = sidebarLocator.GetByRole(AriaRole.Link, new() { Exact = true, Name = buttonTitle });
            await buttonLocator.ClickAsync();
        }

        return resultLocator;
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
