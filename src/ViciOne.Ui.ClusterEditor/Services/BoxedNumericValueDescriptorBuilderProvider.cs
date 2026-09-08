using System;
using System.Diagnostics.CodeAnalysis;
using ViciOne.Ui.ClusterEditor.Builders;

namespace ViciOne.Ui.ClusterEditor.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class BoxedNumericValueDescriptorBuilderProvider(IServiceProvider serviceProvider)
{
    public BoxedNumericValueDescriptorBuilder GetBuilder(Type valueType)
        => new(serviceProvider, valueType);
}
