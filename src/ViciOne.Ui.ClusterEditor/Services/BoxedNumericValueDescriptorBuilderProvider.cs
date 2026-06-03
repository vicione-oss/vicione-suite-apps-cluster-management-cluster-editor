using System;
using ViciOne.Ui.ClusterEditor.Builders;

namespace ViciOne.Ui.ClusterEditor.Services;

internal sealed class BoxedNumericValueDescriptorBuilderProvider(IServiceProvider serviceProvider)
{
    public BoxedNumericValueDescriptorBuilder GetBuilder(Type valueType)
        => new(serviceProvider, valueType);
}
