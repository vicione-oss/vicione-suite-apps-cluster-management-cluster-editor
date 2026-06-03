using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.Ui.Blazor.Components.ContextMenu.Extensions;
using ViciOne.Ui.Blazor.Components.Dialog.Extensions;
using ViciOne.Ui.Blazor.Components.ExpandableMenu.Extensions;
using ViciOne.Ui.Blazor.Components.Popup.Extensions;
using ViciOne.Ui.Blazor.Components.SectionRail.Extensions;
using ViciOne.Ui.ClusterEditor.Components.ContainerBreadcrumb.Extensions;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Components.Scrolling.Extensions;
using ViciOne.Ui.ClusterEditor.Components.ToolbarDataflow.Extensions;
using ViciOne.Ui.ClusterEditor.Components.ToolbarMain.Extensions;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.ComponentStates.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Models.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Sections.Dataflow.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Sections.Debugging.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.Information.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.Library.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.Property.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Extensions;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices.ContainerEditor;
using ViciOne.Ui.Shared.Dx.Services;

namespace ViciOne.Ui.ClusterEditor.Extensions;

public static class IServiceCollectionExtensions
{
    internal static IServiceCollection AddBlockNodeConnectorContextMenu(this IServiceCollection services)
    {
        services.AddContextMenuCore();
        services.AddContextMenuRequest<BlockNodeConnectorContextMenuContext>();
        services.AddContextMenuState<BlockNodeConnectorContextMenuContext, BlockNodeConnectorContextMenuState>();

        return services;
    }

    public static IServiceCollection AddClusterEditor<TRulesetProvider>(this IServiceCollection services, Func<IServiceProvider, TRulesetProvider>? rulesetProviderFactory = null)
        where TRulesetProvider : class, IRulesetProvider
    {
        services.AddScoped<ComparerService>();

        services.AddScoped<BoundsService>();
        services.AddScoped<ClusterBuilderEventBuffer>();
        services.AddScoped<DragService>();
        services.AddScoped<ConnectorSelectionDialogService>();
        services.AddScoped<ConnectorService>();
        services.AddScoped<IDataPortTreeIconProvider, DataPortTreeIconProvider>();
        services.AddScoped<IDatastore, Datastore>();
        services.AddScoped<DiagramEventService>();
        services.AddScoped<DiagramService>();
        services.AddScoped<FullscreenService>();
        services.AddScoped<InputEventService>();
        services.AddScoped<LabelOrderService>();
        services.AddScoped<LinkDestinationDialogService>();
        services.AddScoped<NumericPropertyDescriptorBuilderProvider>();
        services.AddScoped<SelectionManager>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<ToolbarService>();
        services.AddScoped<TooltipService>();
        services.AddScoped<TraceService>();

        services.AddScrolling();

        services.AddPopup();
        services.AddDialog();

        services.AddScoped<IContainerEditorRequest, ContainerEditorRequest>();

        services.AddFbSettingsEditor();

        services.AddBlockNodeConnectorContextMenu();
        services.AddNodeEditorContextMenu();

        services.AddExpandableMenu();
        services.AddSectionRail<DataflowToolbarSection>();

        services.AddDataflowSection();
        services.AddDataPortSection();
        services.AddInformationSection();
        services.AddLibrarySection();
        services.AddPropertySection();
        services.AddPublishedConnectorsSection();
        services.AddTopologySection();

        services.AddContainerEditor();
        services.AddToolbarDataflow();

        services.AddContainerBreadcrumb();

        services.AddMainToolbar();

        services.AddDebugSection();

        services.AddClusterEditorManagement();

        if (rulesetProviderFactory is null)
        {
            services.TryAddScoped<IRulesetProvider, TRulesetProvider>();
        }
        else
        {
            services.AddScoped<IRulesetProvider>(rulesetProviderFactory);
        }

        return services;
    }

    private static IServiceCollection AddClusterEditorManagement(this IServiceCollection services)
    {
        services.AddScoped<ClusterEditorManagement>();
        services.AddScoped<IClusterEditorManagement>(sp => sp.GetRequiredService<ClusterEditorManagement>());
        services.AddScoped<IClusterEditorManagementInternal>(sp => sp.GetRequiredService<ClusterEditorManagement>());
        return services;
    }

    internal static IServiceCollection AddNodeEditorContextMenu(this IServiceCollection services)
    {
        services.AddContextMenuCore();
        services.AddContextMenuRequest<NodeEditorContextMenuContext>();
        services.AddContextMenuState<NodeEditorContextMenuContext, NodeEditorContextMenuState>();

        return services;
    }
}
