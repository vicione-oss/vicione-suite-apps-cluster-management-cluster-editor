using System;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.Topology.Factories;

internal static class SelectableValueCollectionFactory
{
    public static Lazy<SelectableValue<TEnum>[]> CreateLazy<TEnum>()
         where TEnum : struct, Enum
            => new(() =>
            {
                var values = Enum.GetValues<TEnum>();
                var result = new SelectableValue<TEnum>[values.Length];

                for (var i = 0; i < values.Length; i++)
                    result[i] = new SelectableValue<TEnum> { Text = values[i].ToString(), Value = values[i] };

                return result;
            });
}
