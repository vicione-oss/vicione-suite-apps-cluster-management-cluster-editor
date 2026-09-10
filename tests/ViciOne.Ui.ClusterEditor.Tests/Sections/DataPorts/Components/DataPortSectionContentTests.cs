using System.Threading.Tasks;
using Bunit;
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
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupTreeEditorJs();
        ctx.SetupDragService();
        ctx.Services.AddDataPortContextMenu();
        ctx.SetupConnectorService();
        ctx.SetupLinkDestinationDialogService();
        ctx.Services.TryAddScoped(_ => Substitute.For<IRulesetProvider>());
        ctx.Services.TryAddScoped<ToolbarService>();
        ctx.SetupDataPortTreeAdapter();
        ctx.Services.AddDialog();
        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.Render<DataPortSectionContent>();

        // Assert
        Assert.NotNull(component);
    }
}
