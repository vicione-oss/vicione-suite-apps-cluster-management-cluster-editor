using System.Collections.Generic;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;
using ViciOne.Ui.TreeEditor.Builder.Interface.Icons;

namespace ViciOne.Ui.ClusterEditor.Models;

internal class TreeEditorActionButtonMonochromeIcon(MonochromeIconName monochromeIconName) : IIcon
{
    private static readonly MonochromeIconSize s_iconSize = MonochromeIconSize.Small;

    public IEnumerable<string> CssClasses => monochromeIconName.GetCssClasses(s_iconSize);

    public IDictionary<string, string> CssStyles => new Dictionary<string, string>();

    public string MarkupString => string.Empty;

    public string Tooltip => string.Empty;
}
