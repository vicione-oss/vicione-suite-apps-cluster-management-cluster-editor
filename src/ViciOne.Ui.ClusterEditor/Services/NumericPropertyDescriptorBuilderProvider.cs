using System;
using ViciOne.Ui.ClusterEditor.Builders;

namespace ViciOne.Ui.ClusterEditor.Services;

internal sealed class NumericPropertyDescriptorBuilderProvider(IServiceProvider serviceProvider)
{
    public NumericPropertyDescriptorBuilder GetBuilder(Type valueType)
        => new(serviceProvider, valueType);
}
