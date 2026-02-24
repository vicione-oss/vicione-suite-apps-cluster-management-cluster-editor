using System;
using System.Linq;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.Topology.Factories;

internal static class SelectableValueCollectionFactory
{
    public static Lazy<SelectableValue<TEnum>[]> CreateLazy<TEnum>()
         where TEnum : struct, Enum
            => new(() => [.. Enum.GetValues<TEnum>().Select(v => new SelectableValue<TEnum> { Text = v.ToString(), Value = v })]);
}
