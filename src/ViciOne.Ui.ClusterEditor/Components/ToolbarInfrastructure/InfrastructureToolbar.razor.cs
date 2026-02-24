using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.Blazor.Components.ExpandableMenu.Models;
using ViciOne.Ui.Blazor.Components.Sidebar.Enums;
using ViciOne.Ui.ClusterEditor.Localization.Resources;
using ViciOne.Ui.ClusterEditor.Sections.Dataflow.Components;
using ViciOne.Ui.ClusterEditor.Sections.File.Components;
using ViciOne.Ui.ClusterEditor.Sections.Information.Components;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;
using CommonVocabulary = ViciOne.Ui.Localization.Resources.CommonVocabulary;

namespace ViciOne.Ui.ClusterEditor.Components.ToolbarInfrastructure;

public sealed partial class InfrastructureToolbar : ComponentBase
{
    private bool _compactMode;
    private ObservableCollection<ExpandableMenuEntry> _entries = [];
    private int? _sidebarFluidWidth;

    private SidebarMode GetSidebarMode()
        => _compactMode ? SidebarMode.Compact : SidebarMode.Fluid;

    protected override void OnInitialized()
        => _entries =
        [
            new()
            {
                ContentType = typeof(FileSection),
                IconCssClass = MonochromeIconName.HamburgerMenu.GetCssClasses(MonochromeIconSize.SmallMedium).ToSpaceSeparated(),
                Label = CommonVocabulary.File,
            },
            new()
            {
                ContentType = typeof(DataflowSection),
                IconCssClass = MonochromeIconName.DataflowSolid.GetCssClasses(MonochromeIconSize.SmallMedium).ToSpaceSeparated(),
                IsDefault = true,
                Label = TechnicalTerms.Dataflow,
            },
            new()
            {
                ContentType = typeof(InformationSection),
                IconCssClass = MonochromeIconName.InfoOutlined.GetCssClasses(MonochromeIconSize.SmallMedium).ToSpaceSeparated(),
                IsSticky = true,
                Label = DataflowToolbarSectionNames.Information,
            },
        ];
}
