using System.Collections.Generic;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;
using ViciOne.Ui.TreeEditor.Builder.Interface.Icons;

namespace ViciOne.Ui.ClusterEditor.Models;

internal class TreeEditorMonochromeIcon(MonochromeIconName monochromeIconName, MonochromeIconSize iconSize) : IIcon
{
    public IEnumerable<string> CssClasses => monochromeIconName.GetCssClasses(iconSize);

    public IDictionary<string, string> CssStyles => new Dictionary<string, string>();

    public string MarkupString => string.Empty;

    public string Tooltip => string.Empty;
}
