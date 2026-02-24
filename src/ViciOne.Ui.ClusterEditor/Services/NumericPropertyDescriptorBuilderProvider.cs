using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.Blazor.Components.Extensions;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services.TypeDescriptors;

namespace ViciOne.Ui.ClusterEditor.Services;

internal sealed class NumericPropertyDescriptorBuilderProvider(IServiceProvider serviceProvider)
{
    public NumericPropertyDescriptorBuilder GetBuilder(Type valueType)
        => new(serviceProvider, valueType);
}

internal sealed class NumericPropertyDescriptorBuilder(IServiceProvider serviceProvider, Type valueType)
{
    private object? _interval;
    private Type _intervalType = valueType.MakeNonNullableType();
    private Type _limitType = valueType.MakeNonNullableType();
    private object? _maximum;
    private object? _minimum;
    private readonly Type _valueType = valueType;

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
        var interval = _interval;
        if (interval is null)
        {
            var getIntervalFromNumericDescriptorDelegate = GetIntervalFor<int>;
            var getIntervalFromNumericDescriptorMethod = getIntervalFromNumericDescriptorDelegate.Method
                .GetGenericMethodDefinition()
                .MakeGenericMethod(_intervalType);

            interval = getIntervalFromNumericDescriptorMethod.Invoke(this, []);
            if (interval is null)
                return null;
        }
        var minimum = _minimum;
        if (minimum is null)
        {
            var getMinimumFromNumericDescriptorDelegate = GetMinimumFor<int>;
            var getMinimumFromNumericDescriptorMethod = getMinimumFromNumericDescriptorDelegate.Method
                .GetGenericMethodDefinition()
                .MakeGenericMethod(_limitType);

            minimum = getMinimumFromNumericDescriptorMethod.Invoke(this, []);
            if (minimum is null)
                return null;
        }

        var maximum = _maximum;
        if (maximum is null)
        {
            var getMaximumFromNumericDescriptorDelegate = GetMaximumFor<int>;
            var getMaximumFromNumericDescriptorMethod = getMaximumFromNumericDescriptorDelegate.Method
                .GetGenericMethodDefinition()
                .MakeGenericMethod(_limitType);

            maximum = getMaximumFromNumericDescriptorMethod.Invoke(this, []);
            if (maximum is null)
                return null;
        }

        var methodInfo = createPropertyDescriptorDelegate.Method.GetGenericMethodDefinition()
            .MakeGenericMethod(typeof(TInstance), _valueType, _intervalType, _limitType);

        var parameters = new List<object?> { interval, minimum, maximum };
        if (additionalParameters is not null)
            parameters.AddRange(additionalParameters);

        var invokeResult = methodInfo.Invoke(createPropertyDescriptorDelegate.Target, [.. parameters]);

        var propertyDescriptor = invokeResult as IPropertyDescriptor<TInstance>;

        return propertyDescriptor;
    }

    private TPropertyValue? GetIntervalFor<TPropertyValue>()
    {
        var numericValueTypeDescriptor = serviceProvider.GetService<INumericValueTypeDescriptor<TPropertyValue>>();
        if (numericValueTypeDescriptor is null)
            return default;

        return numericValueTypeDescriptor.One;
    }

    private TPropertyValue? GetMaximumFor<TPropertyValue>()
    {
        var numericValueTypeDescriptor = serviceProvider.GetService<INumericValueTypeDescriptor<TPropertyValue>>();
        if (numericValueTypeDescriptor is null)
            return default;

        return numericValueTypeDescriptor.Maximum;
    }

    private TPropertyValue? GetMinimumFor<TPropertyValue>()
    {
        var numericValueTypeDescriptor = serviceProvider.GetService<INumericValueTypeDescriptor<TPropertyValue>>();
        if (numericValueTypeDescriptor is null)
            return default;

        return numericValueTypeDescriptor.Minimum;
    }

    public NumericPropertyDescriptorBuilder WithInterval(object interval)
    {
        var intervalType = interval.GetType();

        if (intervalType != _intervalType)
            throw new ArgumentException("Passed interval does not match the interval type.", nameof(interval));

        _interval = interval;

        return this;
    }

    public NumericPropertyDescriptorBuilder WithIntervalType(Type intervalType)
    {
        _intervalType = intervalType;
        _interval = null;

        return this;
    }

    public NumericPropertyDescriptorBuilder WithLimitType(Type limitType)
    {
        _limitType = limitType;
        _minimum = null;
        _maximum = null;

        return this;
    }

    public NumericPropertyDescriptorBuilder WithMaximum(object maximum)
    {
        var maximumType = maximum.GetType();

        if (maximumType != _limitType)
            throw new ArgumentException("Passed maximum does not match the limit type.", nameof(maximum));

        _maximum = maximum;

        return this;
    }

    public NumericPropertyDescriptorBuilder WithMinimum(object minimum)
    {
        var minimumType = minimum.GetType();

        if (minimumType != _limitType)
            throw new ArgumentException("Passed minimum does not match the limit type.", nameof(minimum));

        _minimum = minimum;

        return this;
    }
}
