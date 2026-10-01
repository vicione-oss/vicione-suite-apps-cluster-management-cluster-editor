using System.Threading.Tasks;
using Bunit;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;
using FbSettingsEditorComponent = ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor.FbSettingsEditor;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.FbSettingsEditor;

public class FbSettingsEditorTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupFbSettingsEditor();

        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.Render<FbSettingsEditorComponent>();

        // Assert
        Assert.NotNull(component);
    }
}
