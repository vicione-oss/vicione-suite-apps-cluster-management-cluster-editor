using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Components.Rendering;

namespace ViciOne.Ui.ClusterEditor.Components.UniversalInput.Extensions;

internal static class RenderTreeBuilderExtension
{
    private static object ConvertValue(object value, Type type)
        => Convert.ChangeType(value, type, CultureInfo.InvariantCulture);

    public static int SetAdditionalProperties(this RenderTreeBuilder renderTreeBuilder, int sequence, Dictionary<string, object>? propertyBag, Type controlType)
    {
        if (propertyBag is null)
            return sequence;

        foreach (var attribute in propertyBag)
        {
            var props = controlType.GetProperties();
            var property = props.FirstOrDefault(pi => pi.Name == attribute.Key);

            if (property is not null)
            {
                var value = attribute.Value;

                if (property.PropertyType != attribute.Value.GetType())
                {
                    value = ConvertValue(attribute.Value, property.PropertyType);
                }

                renderTreeBuilder.AddAttribute(sequence++, attribute.Key, value);
            }
        }

        return sequence;
    }
}
