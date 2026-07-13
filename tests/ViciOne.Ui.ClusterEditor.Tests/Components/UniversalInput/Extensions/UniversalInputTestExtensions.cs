using System.Linq;
using AngleSharp.Common;
using AngleSharp.Html.Dom;
using Bunit;
using ViciOne.Ui.Blazor.Components.ComboBox;
using ViciOne.Ui.Blazor.Components.TextBox;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.UniversalInput.Extensions;

internal static class UniversalInputTestExtensions
{
    public static void SelectItemAtIndex<TItem, TValue>(this IRenderedComponent<ComboBox<TItem, TValue>> cbo, int idx)
    {
        var dropdownItems = cbo.WaitForElements(".drop-down-item").OfType<IHtmlDivElement>();

        var dropdownItem = dropdownItems.GetItemByIndex(idx);

        dropdownItem.Click();
    }

    public static void TextEditChange(this IRenderedComponent<TextBox> textbox, string text)
    {
        var input = textbox.Find("input");

        input.Input(text);
        input.KeyUp(Key.Enter);
    }
}
