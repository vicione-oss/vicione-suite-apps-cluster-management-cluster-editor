using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;

namespace ViciOne.Ui.ClusterEditor.Components.ContextMenu;


[System.Diagnostics.CodeAnalysis.SuppressMessage("Ordering", "VO2002:Wrong field order", Justification = "s_iconSize cannot be ordered alphabetically, as this would result in it being referenced before its definition")]
internal static class ContextMenuIcon
{
    private static readonly MonochromeIconSize s_iconSize = MonochromeIconSize.Small;

    private static readonly string s_addContainer = MonochromeIconName.ContainerAdd.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_addLabel = MonochromeIconName.AddTextLabelsSolid.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_alignBottomIcon = MonochromeIconName.AlignBottom.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_alignLeftIcon = MonochromeIconName.AlignLeft.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_alignRightIcon = MonochromeIconName.AlignRight.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_alignTopByLowestElementIcon = MonochromeIconName.AlignTopByLowestElement.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_alignTopIcon = MonochromeIconName.AlignTop.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_apply = MonochromeIconName.Check.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_connectorAddToContainer = MonochromeIconName.ConnectorAddToContainer.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_connectorCancelPublication = MonochromeIconName.ConnectorCancelPublication.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_connectorPublish = MonochromeIconName.ConnectorPublish.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_connectorRemoveFromContainer = MonochromeIconName.ConnectorRemoveFromContainer.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_connectorReset = MonochromeIconName.Reload.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_containerDissolve = MonochromeIconName.ContainerDissolveSolid.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_containerEdit = MonochromeIconName.Edit.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_delete = MonochromeIconName.Delete.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_lockTextLabels = MonochromeIconName.LockTextLabelsSolid.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_moveToNewContainer = MonochromeIconName.MoveToNewContainer.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_selectAllBlocks = MonochromeIconName.SelectAllBlocks.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_selectAllFunctionBlockConnectors = MonochromeIconName.SelectConnectorsInputAndOutputAll.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_selectAllFunctionBlockInputConnectors = MonochromeIconName.SelectConnectorsInputAll.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_selectAllFunctionBlockOutputConnectors = MonochromeIconName.SelectConnectorsOutputAll.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_selectFunctionBlockInputConnectors = MonochromeIconName.SelectConnectorsInput.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_selectFunctionBlockInputOutputConnectors = MonochromeIconName.SelectConnectorsInputAndOutput.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_selectFunctionBlockOutputConnectors = MonochromeIconName.SelectConnectorsOutput.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_settingsEdit = MonochromeIconName.GearSolid.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_substract = MonochromeIconName.MinusSlim.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_textLabelBringToFront = MonochromeIconName.SeveralToFront.GetCssClasses(s_iconSize).ToSpaceSeparated();
    private static readonly string s_textLabelSendToBack = MonochromeIconName.SeveralToBack.GetCssClasses(s_iconSize).ToSpaceSeparated();

    internal static string AddContainer => s_addContainer;
    internal static string AddLabel => s_addLabel;
    internal static string AlignBottomIcon => s_alignBottomIcon;
    internal static string AlignLeftIcon => s_alignLeftIcon;
    internal static string AlignRightIcon => s_alignRightIcon;
    internal static string AlignTopByLowestElementIcon => s_alignTopByLowestElementIcon;
    internal static string AlignTopIcon => s_alignTopIcon;
    internal static string Apply => s_apply;
    internal static string ConnectorAddToContainer => s_connectorAddToContainer;
    internal static string ConnectorCancelPublication => s_connectorCancelPublication;
    internal static string ConnectorPublish => s_connectorPublish;
    internal static string ConnectorRemoveFromContainer => s_connectorRemoveFromContainer;
    internal static string ConnectorReset => s_connectorReset;
    internal static string ContainerDissolve => s_containerDissolve;
    internal static string ContainerEdit => s_containerEdit;
    internal static string Delete => s_delete;
    internal static string LockTextLabels => s_lockTextLabels;
    internal static string MoveToNewContainer => s_moveToNewContainer;
    internal static string SelectAllBlocks => s_selectAllBlocks;
    internal static string SelectAllFunctionBlockConnectors => s_selectAllFunctionBlockConnectors;
    internal static string SelectAllFunctionBlockInputConnectors => s_selectAllFunctionBlockInputConnectors;
    internal static string SelectAllFunctionBlockOutputConnectors => s_selectAllFunctionBlockOutputConnectors;
    internal static string SelectFunctionBlockInputConnectors => s_selectFunctionBlockInputConnectors;
    internal static string SelectFunctionBlockInputOutputConnectors => s_selectFunctionBlockInputOutputConnectors;
    internal static string SelectFunctionBlockOutputConnectors => s_selectFunctionBlockOutputConnectors;
    internal static string SettingsEdit => s_settingsEdit;
    internal static string Substract => s_substract;
    internal static string TextLabelBringToFront => s_textLabelBringToFront;
    internal static string TextLabelSendToBack => s_textLabelSendToBack;
}
