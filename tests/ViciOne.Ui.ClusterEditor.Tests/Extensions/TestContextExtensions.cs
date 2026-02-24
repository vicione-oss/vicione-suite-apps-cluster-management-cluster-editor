using Blazor.Diagrams;
using Blazor.Diagrams.Core.Models;
using Bunit;
using DevExpress.Blazor.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Sections.Dataflow.Services;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Sections.Information.Services;
using ViciOne.Ui.ClusterEditor.Sections.Library.Services;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.Shared.Dx.Components.Resizing;
using ViciOne.Ui.Shared.Dx.Services;
using Xunit;
using TestContext = Bunit.TestContext;

namespace ViciOne.Ui.ClusterEditor.Tests.Extensions;

internal static class TestContextExtensions
{
    public static BlockNodeConnector CreateBlockNodeConnector(this TestContext ctx)
    {
        var connector = Substitute.For<Cluster.Model.IConnector>();
        connector.Links.Returns([]);

        var comparer = ctx.Services.GetRequiredService<ComparerService>();
        var datastore = ctx.Services.GetRequiredService<Datastore>();
        var diagramService = ctx.Services.GetRequiredService<DiagramService>();
        var node = new FunctionBlockNode();

        return new BlockNodeConnector(comparer, connector, datastore, diagramService, node, true);
    }

    public static BlockNodeLink CreateBlockNodeLink(this TestContext _)
    {
        var node = new LabelNode(new(0, 0));

        return new BlockNodeLink(new PortModel(node, PortAlignment.Top), new PortModel(node, PortAlignment.Bottom));
    }

    public static TestContext CreateDiagramInstance(this TestContext ctx)
    {
        var service = ctx.Services.GetRequiredService<DiagramService>();
        service.Diagram = new BlazorDiagram();

        return ctx;
    }

    public static TestContext SetupBoundsService(this TestContext ctx)
    {
        ctx.Services.TryAddScoped<BoundsService>();
        return ctx;
    }

    public static TestContext SetupConnectorSelectionDialogService(this TestContext ctx)
    {
        ctx.SetupDatastore();
        ctx.SetupSelectionManager();

        ctx.Services.TryAddScoped<ConnectorSelectionDialogService>();

        return ctx;
    }

    public static TestContext SetupConnectorService(this TestContext ctx)
    {
        ctx.SetupSelectionManager();
        ctx.SetupDiagramService();

        ctx.Services.TryAddScoped<ConnectorService>();

        return ctx;
    }

    public static TestContext SetupDataflowStructureTreeAdapter(this TestContext ctx)
    {
        ctx.SetupSelectionManager();

        ctx.Services.TryAddScoped<DataflowStructureTreeAdapter>();

        return ctx;
    }

    public static TestContext SetupDataManagementService(this TestContext ctx)
    {
        ctx.Services.TryAddScoped<LibraryService>();
        ctx.SetupDiagramService();

        ctx.Services.TryAddScoped<DataManagementService>();

        return ctx;
    }

    public static TestContext SetupDataPortTreeAdapter(this TestContext ctx)
    {
        ctx.Services.AddDataPortAddChildNodeContextMenu();
        ctx.Services.TryAddScoped(_ => Substitute.For<IDataManagementService>());
        ctx.SetupDatastore();
        ctx.Services.TryAddScoped<IDataPortTreeIconProvider, DataPortTreeIconProvider>();

        ctx.Services.TryAddScoped<DataPortTreeAdapter>();

        return ctx;
    }

    public static TestContext SetupDatastore(this TestContext ctx)
    {
        ctx.Services.TryAddSingleton(new ComparerService([], Substitute.For<ILogger<ComparerService>>()));
        ctx.Services.TryAddScoped<DiagramEventService>();
        ctx.Services.TryAddScoped<ClusterBuilderEventBuffer>();

        ctx.Services.TryAddScoped<Datastore>();

        return ctx;
    }

    public static TestContext SetupDevExpressBlazor(this TestContext ctx)
    {
        var deviceInfo = new DeviceInfo(false);
        var env = Substitute.For<IEnvironmentInfo>();
        env.DeviceInfo.Returns(deviceInfo);
        ctx.Services.AddScoped(s => env);

        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        ctx.Services.AddDevExpressBlazor(options => options.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5);
        var rootModule = ctx.JSInterop.SetupModule("./_content/DexExpress.Blazor/dx-blazor.js");
        rootModule.Mode = JSRuntimeMode.Strict;
        rootModule.Setup<DeviceInfo>("getDeviceInfo", _ => true).SetResult(deviceInfo);

        return ctx;
    }

    public static TestContext SetupDiagramService(this TestContext ctx)
    {
        ctx.SetupDatastore();

        ctx.Services.TryAddScoped<DiagramService>();

        return ctx;
    }

    public static TestContext SetupDragService(this TestContext ctx)
    {
        ctx.Services.TryAddScoped<InputEventService>();
        ctx.Services.TryAddScoped<DiagramEventService>();
        ctx.SetupDiagramService();

        ctx.Services.TryAddScoped<DragService>();

        return ctx;
    }

    public static TestContext SetupLinkDestinationDialogService(this TestContext ctx)
    {
        ctx.SetupDatastore();

        ctx.Services.TryAddScoped<LinkDestinationDialogService>();

        return ctx;
    }

    public static TestContext SetupPublishedConnectorsService(this TestContext ctx)
    {
        ctx.SetupDiagramService();
        ctx.Services.TryAddScoped<InputEventService>();

        ctx.Services.TryAddScoped<PublishedConnectorsService>();

        return ctx;
    }

    public static TestContext SetupResizeObserver(this TestContext ctx)
    {
        ctx.JSInterop.SetupVoid("ViciOne.Observer.observe", _ => true);

        ctx.Services.TryAddScoped(_ => Substitute.For<IResizeObserver>());

        return ctx;
    }

    public static TestContext SetupSelectionManager(this TestContext ctx)
    {
        ctx.SetupDiagramService();

        ctx.Services.TryAddScoped<SelectionManager>();

        return ctx;
    }

    public static TestContext SetupStatisticService(this TestContext ctx)
    {
        ctx.SetupDatastore();

        ctx.Services.TryAddScoped<StatisticService>();

        return ctx;
    }

    public static TestContext SetupTreeEditorJs(this TestContext ctx)
    {
        var assemblyName = typeof(TreeEditor.TreeEditor).Assembly.GetName();
        Assert.NotNull(assemblyName);

        var module = ctx.JSInterop.SetupModule($"./_content/{assemblyName.Name}/{nameof(TreeEditor.TreeEditor)}.razor.js");
        module.Mode = JSRuntimeMode.Strict;

        return ctx;
    }
}
