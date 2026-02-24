using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.FbSettingsEditor;

public class FbSettingsEditorTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDevExpressBlazor();
        ctx.SetupDatastore();
        ctx.Services.TryAddScoped(_ => Substitute.For<IFbSettingsEditorRequest>());
        ctx.Services.TryAddScoped<FullscreenService>();
        ctx.SetupSelectionManager();

        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.RenderComponent<ClusterEditor.Components.FbSettingsEditor.FbSettingsEditor>();

        // Assert
        Assert.NotNull(component);
    }
}
