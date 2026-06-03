using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.Blazor.Components.Extensions;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services.TypeDescriptors;
using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Builders;

internal sealed class BoxedNumericValueDescriptorBuilder(IServiceProvider serviceProvider, Type valueType)
{
    private object? _interval;
    private readonly Type _intervalType = valueType.MakeNonNullableType();
    private readonly Type _limitType = valueType.MakeNonNullableType();
    private object? _maximum;
    private object? _minimum;
    private readonly Type _valueType = valueType;

    public BoxedNumericValueDescriptor? Build()
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

        var descriptor = new BoxedNumericValueDescriptor
        {
            Interval = interval,
            IntervalType = _intervalType,
            LimitType = _limitType,
            Maximum = maximum,
            Minimum = minimum,
            ValueType = _valueType
        };

        return descriptor;
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

    public BoxedNumericValueDescriptorBuilder WithInterval(object interval)
    {
        var intervalType = interval.GetType();

        if (intervalType != _intervalType)
            throw new ArgumentException("Passed interval does not match the interval type.", nameof(interval));

        _interval = interval;

        return this;
    }

    public BoxedNumericValueDescriptorBuilder WithMaximum(object maximum)
    {
        var maximumType = maximum.GetType();

        if (maximumType != _limitType)
            throw new ArgumentException("Passed maximum does not match the limit type.", nameof(maximum));

        _maximum = maximum;

        return this;
    }

    public BoxedNumericValueDescriptorBuilder WithMinimum(object minimum)
    {
        var minimumType = minimum.GetType();

        if (minimumType != _limitType)
            throw new ArgumentException("Passed minimum does not match the limit type.", nameof(minimum));

        _minimum = minimum;

        return this;
    }
}
