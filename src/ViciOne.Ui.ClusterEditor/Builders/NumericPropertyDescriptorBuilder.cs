using System;
using System.Collections.Generic;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;

namespace ViciOne.Ui.ClusterEditor.Builders;

internal sealed class NumericPropertyDescriptorBuilder(IServiceProvider serviceProvider, Type valueType)
{
    private readonly BoxedNumericValueDescriptorBuilder _builder = new(serviceProvider, valueType);

    /// <summary>
    /// Builds a numeric property descriptor from the current state of the builder.
    /// </summary>
    /// <param name="createPropertyDescriptorDelegate">
    /// Delegate of type <see cref="Func{T1, T2, T3, T4, TResult}"/>
    /// 
    /// <para>T1 = <typeparamref name="TInstance"/></para>
    /// <para>T2 = <typeparamref name="TValue"/></para>
    /// <para>T3 = <typeparamref name="TInterval"/></para>
    /// <para>T4 = <typeparamref name="TLimit"/></para>
    /// <para>TResult = numeric property descriptor</para>
    /// </param>
    /// <param name="additionalParameters">
    /// Additional parameters passed to <paramref name="createPropertyDescriptorDelegate"/> next to interval, minimum and maximum
    /// </param>
    public IPropertyDescriptor<TInstance>? Build<TInstance>(Delegate createPropertyDescriptorDelegate,
        params object?[] additionalParameters)
    {
        var boxedNumericValueDescriptor = _builder.Build();
        if (boxedNumericValueDescriptor is null)
            return null;

        var methodInfo = createPropertyDescriptorDelegate.Method.GetGenericMethodDefinition()
            .MakeGenericMethod(typeof(TInstance),
                boxedNumericValueDescriptor.ValueType,
                boxedNumericValueDescriptor.IntervalType,
                boxedNumericValueDescriptor.LimitType);

        var parameters = new List<object?>
        {
            boxedNumericValueDescriptor.Interval,
            boxedNumericValueDescriptor.Minimum,
            boxedNumericValueDescriptor.Maximum
        };

        if (additionalParameters is not null)
            parameters.AddRange(additionalParameters);

        var invokeResult = methodInfo.Invoke(createPropertyDescriptorDelegate.Target, [.. parameters]);

        var propertyDescriptor = invokeResult as IPropertyDescriptor<TInstance>;

        return propertyDescriptor;
    }

    public NumericPropertyDescriptorBuilder WithInterval(object interval)
    {
        _builder.WithInterval(interval);

        return this;
    }

    public NumericPropertyDescriptorBuilder WithMaximum(object maximum)
    {
        _builder.WithMaximum(maximum);

        return this;
    }

    public NumericPropertyDescriptorBuilder WithMinimum(object minimum)
    {
        _builder.WithMinimum(minimum);

        return this;
    }
}
