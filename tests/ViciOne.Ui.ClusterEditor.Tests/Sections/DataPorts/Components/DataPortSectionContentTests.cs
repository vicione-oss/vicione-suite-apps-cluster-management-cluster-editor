using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ViciOne.Ui.Blazor.Components.Dialog.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Components;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Components;

public class DataPortSectionContentTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupTreeEditorJs();
        ctx.SetupDragService();
        ctx.Services.AddDataPortContextMenu();
        ctx.SetupConnectorService();
        ctx.SetupLinkDestinationDialogService();
        ctx.Services.TryAddScoped(_ => Substitute.For<IRulesetProvider>());
        ctx.Services.TryAddScoped<ToolbarService>();
        ctx.SetupDataPortTreeAdapter();
        ctx.Services.AddDialog();

        // Act
        var component = ctx.RenderComponent<DataPortSectionContent>();

        // Assert
        Assert.NotNull(component);
    }
}
