using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Bunit;
using DevExpress.Blazor.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.Blazor.Components.Dialog.Extensions;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.Blazor.Components.Resizing.Services;
using ViciOne.Ui.ClusterEditor.Components;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor;
using ViciOne.Ui.ClusterEditor.Components.NodeEditorServices;
using ViciOne.Ui.ClusterEditor.Components.ToolbarDataflow.Models;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Sections.Dataflow.Services;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Sections.Information.Services;
using ViciOne.Ui.ClusterEditor.Sections.Library.Services;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.Shared.Dx.Services;
using Xunit;
using TestContext = Bunit.TestContext;

namespace ViciOne.Ui.ClusterEditor.Tests.Extensions;

internal static class TestContextExtensions
{
    public static BlockNodeConnector CreateBlockNodeConnector(this TestContext ctx, BlockNode? node = null, bool isInput = true)
    {
        var connector = Substitute.For<Cluster.Model.IConnector>();
        connector.Links.Returns([]);

        var comparer = ctx.Services.GetRequiredService<ComparerService>();
        var datastore = ctx.Services.GetRequiredService<IDatastore>();
        var diagramService = ctx.Services.GetRequiredService<DiagramService>();

        return new BlockNodeConnector(comparer, connector, datastore, diagramService, node ?? new FunctionBlockNode(), isInput);
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

    public static FakeLogger<T> GetFakeLogger<T>(this TestContext ctx)
        => (FakeLogger<T>)ctx.Services.GetRequiredService<ILogger<T>>();

    public static TestContext SetupBoundsService(this TestContext ctx)
    {
        ctx.Services.TryAddScoped<BoundsService>();
        return ctx;
    }

    public static TestContext SetupClusterEditorManagement(this TestContext ctx)
    {
        ctx.SetupLibraryService();
        ctx.SetupDiagramService();

        ctx.Services.TryAddScoped<ClusterEditorManagement>();
        ctx.Services.TryAddScoped<IClusterEditorManagement>(sp => sp.GetRequiredService<ClusterEditorManagement>());
        ctx.Services.TryAddScoped<IClusterEditorManagementInternal>(sp => sp.GetRequiredService<ClusterEditorManagement>());

        return ctx;
    }

    public static TestContext SetupComboBox(this TestContext ctx)
        => ctx.SetupDropDown();

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

    public static TestContext SetupDataPortTreeAdapter(this TestContext ctx)
    {
        ctx.Services.AddDataPortAddChildNodeContextMenu();
        ctx.SetupClusterEditorManagement();
        ctx.SetupDatastore();

        ctx.Services.TryAddScoped<DataPortTreeAdapter>();
        ctx.Services.TryAddScoped<DataPortTreeBuilderRegistry>();
        ctx.Services.TryAddScoped<DataPortIconResolver>();
        ctx.Services.TryAddScoped<DataPortTreeState>();
        ctx.Services.TryAddScoped<DataPortEditingCoordinator>();
        ctx.Services.TryAddScoped<DataPortTreeMutator>();
        ctx.Services.TryAddScoped<DataPortNodeActionProvider>();
        ctx.Services.TryAddScoped<DataPortDragCoordinator>();
        ctx.Services.TryAddScoped<DataPortClusterEventSynchronizer>();

        return ctx;
    }

    public static TestContext SetupDatastore(this TestContext ctx)
    {
        ctx.Services.TryAddSingleton(new ComparerService([], Substitute.For<ILogger<ComparerService>>()));
        ctx.Services.TryAddScoped<DiagramEventService>();
        ctx.Services.TryAddScoped<ClusterBuilderEventBuffer>();

        ctx.Services.AddDatastore();

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

    public static TestContext SetupDropDown(this TestContext ctx)
    {
        var dropDownModule = ctx.JSInterop.SetupModule("./_content/ViciOne.Ui.Blazor.Components/drop-down/drop-down.js");
        var dropDownInstance = dropDownModule.SetupModule("attach", _ => true);
        dropDownInstance.SetupVoid("attachInputElement", _ => true);
        dropDownInstance.SetupVoid("reserveWidth");
        dropDownInstance.SetupVoid("setMinimumWidth");

        return ctx;
    }

    public static TestContext SetupFbSettingsEditor(this TestContext ctx)
    {
        ctx.SetupDatastore();
        ctx.SetupSelectionManager();
        ctx.Services.TryAddScoped(_ => Substitute.For<IFbSettingsEditorRequest>());
        ctx.Services.TryAddScoped<FullscreenService>();
        ctx.Services.AddDialog();

        ctx.JSInterop.SetupModule("./_content/ViciOne.Ui.ClusterEditor/Components/FbSettingsEditor/FbSettingsEditor.razor.js");

        return ctx;
    }

    public static TestContext SetupLabelEditor(this TestContext ctx)
    {
        ctx.Services.AddDialog();

        ctx.JSInterop.SetupModule("./_content/ViciOne.Ui.ClusterEditor/Components/LabelEditor.razor.js");

        return ctx;
    }

    public static TestContext SetupLibraryService(this TestContext ctx)
    {
        ctx.Services.TryAddScoped<ILibraryService, LibraryService>();

        return ctx;
    }

    public static TestContext SetupLinkDestinationDialogService(this TestContext ctx)
    {
        ctx.SetupDatastore();

        ctx.Services.TryAddScoped<LinkDestinationDialogService>();

        return ctx;
    }

    public static TestContext SetupNodeEditor(this TestContext ctx)
    {
        ctx.SetupLabelEditor();
        ctx.SetupFbSettingsEditor();
        ctx.SetupDatastore();
        ctx.SetupDragService();
        ctx.SetupConnectorService();
        ctx.Services.TryAddScoped(_ => Substitute.For<IContextMenuRequest<NodeEditorContextMenuContext>>());
        ctx.SetupLibraryService();
        ctx.SetupResizeObserver();
        ctx.SetupPublishedConnectorsService();
        ctx.SetupConnectorSelectionDialogService();
        ctx.SetupLinkDestinationDialogService();
        ctx.Services.TryAddScoped(_ => Substitute.For<IContainerEditorRequest>());
        ctx.Services.TryAddScoped(_ => Substitute.For<IFbSettingsEditorRequest>());
        ctx.Services.TryAddScoped<FullscreenService>();
        ctx.Services.TryAddScoped<TooltipService>();
        ctx.Services.AddContainerEditor();
        ctx.Services.TryAddScoped(_ => Substitute.For<IPropertyGridController<DataflowToolbarPropertyGridContext>>());
        ctx.Services.TryAddSingleton<ILogger<NodeEditor>>(new FakeLogger<NodeEditor>());
        ctx.Services.TryAddScoped<NodeEditorBehaviorController>();
        ctx.Services.TryAddScoped<DiagramPointerInteractionController>();
        ctx.Services.TryAddScoped<DiagramModelSyncController>();
        ctx.Services.TryAddScoped<LibraryGhostDragController>();
        ctx.Services.TryAddScoped<ConnectorMarkerDeletionController>();
        ctx.Services.TryAddScoped<LabelEditingController>();
        ctx.Services.TryAddScoped<NodeEditorJsInterop>();

        ctx.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true);

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
