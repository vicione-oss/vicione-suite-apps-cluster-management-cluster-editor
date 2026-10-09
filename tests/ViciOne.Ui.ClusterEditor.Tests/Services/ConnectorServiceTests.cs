using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Blazor.Diagrams.Core.Geometry;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using ViciOne.Ui.ClusterEditor.Tests.TestHelpers;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Services;

public class ConnectorServiceTests
{
    private static CancellationToken Ct => Xunit.TestContext.Current.CancellationToken;

    [Fact]
    public async Task ShowAndSelectConnector_WithConnectorOfLoadedCluster_SelectsItsDiagramModel()
    {
        // Arrange
        await using var ctx = CreateContext();
        using var builder = BuilderFactory.Create();
        var connector = await LoadWithFunctionBlockConnectorAsync(ctx, builder);
        var sut = ctx.Services.GetRequiredService<ConnectorService>();

        // Act
        await sut.ShowAndSelectConnector(connector);

        // Assert
        var nodeConnector = ctx.Services.GetRequiredService<IDatastore>().DataflowDiagramMapping.GetDiagramModel(connector);

        ctx.Services.GetRequiredService<SelectionManager>().IsSelected(nodeConnector).Should().BeTrue();
    }

    [Fact]
    public async Task ShowAndSelectConnector_WithConnectorOfReplacedCluster_DoesNotThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        using var builder = BuilderFactory.Create();
        var staleConnector = await LoadWithFunctionBlockConnectorAsync(ctx, builder);
        using var reloaded = BuilderFactory.CreateReloaded(builder);
        await ctx.Services.GetRequiredService<IDatastore>().Load(reloaded, ctx.Services.GetRequiredService<DiagramService>(), Ct);
        var sut = ctx.Services.GetRequiredService<ConnectorService>();

        // Act
        var act = async () => await sut.ShowAndSelectConnector(staleConnector);

        // Assert
        await act.Should().NotThrowAsync();
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop
            .Setup<int[]>("ViciOne.Diagram.BlockNode.measureNameFieldHeights", _ => true)
            .SetResult([.. Enumerable.Repeat(0, 10)]);

        ctx.SetupConnectorService();
        ctx.CreateDiagramInstance();

        return ctx;
    }

    private static async Task<IConnector> LoadWithFunctionBlockConnectorAsync(BunitContext ctx, IClusterBuilder builder)
    {
        var datastore = ctx.Services.GetRequiredService<IDatastore>();
        var diagramService = ctx.Services.GetRequiredService<DiagramService>();

        await datastore.Load(builder, diagramService, Ct);

        var node = await datastore.AddFunctionBlock(diagramService, BuilderFactory.FbDesignId, new Point(0, 0), Ct);

        return datastore.DataflowDiagramMapping.GetModel(node).Outputs.First(c => c.Name == "Value");
    }
}
