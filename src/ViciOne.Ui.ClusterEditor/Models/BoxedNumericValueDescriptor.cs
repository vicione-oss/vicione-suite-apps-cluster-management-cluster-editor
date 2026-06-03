using System;

namespace ViciOne.Ui.ClusterEditor.Models;

internal sealed class BoxedNumericValueDescriptor
{
    public object? Interval { get; set; }
    public required Type IntervalType { get; set; }
    public required Type LimitType { get; set; }
    public object? Maximum { get; set; }
    public object? Minimum { get; set; }
    public required Type ValueType { get; set; }
}
