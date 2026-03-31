using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.Blazor.Components.Dialog.Extensions;
using ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

public class FbSettingsEditorTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDevExpressBlazor();
        ctx.SetupDatastore();
        ctx.SetupSelectionManager();
        ctx.Services.TryAddScoped(_ => Substitute.For<IContextMenuSettings>());
        ctx.Services.TryAddScoped(_ => Substitute.For<IFbSettingsEditorRequest>());
        ctx.Services.TryAddScoped<FullscreenService>();
        ctx.Services.AddDialog();

        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.RenderComponent<FbSettingsEditor>();

        // Assert
        Assert.NotNull(component);
    }
}
