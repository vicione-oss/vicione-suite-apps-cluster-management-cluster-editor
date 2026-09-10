using System.Threading.Tasks;
using Bunit;
using ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

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
        var component = ctx.Render<FbSettingsEditor>();

        // Assert
        Assert.NotNull(component);
    }
}
