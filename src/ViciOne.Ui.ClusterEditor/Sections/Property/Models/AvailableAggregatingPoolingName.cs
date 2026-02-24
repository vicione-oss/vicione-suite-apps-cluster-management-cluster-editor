namespace ViciOne.Ui.ClusterEditor.Sections.Property.Models;

[StronglyTypedId(backingType: StronglyTypedIdBackingType.String, jsonConverter: StronglyTypedIdJsonConverter.SystemTextJson)]
public readonly partial struct AvailableAggregatingPoolingName
{
    public AvailableAggregatingPoolingName()
        => Value = string.Empty; // assign empty to avoid possible null-reference exception in generated Equals()

    public static bool operator <(AvailableAggregatingPoolingName left, AvailableAggregatingPoolingName right)
        => left.CompareTo(right) < 0;

    public static bool operator <=(AvailableAggregatingPoolingName left, AvailableAggregatingPoolingName right)
        => left.CompareTo(right) <= 0;

    public static bool operator >(AvailableAggregatingPoolingName left, AvailableAggregatingPoolingName right)
        => left.CompareTo(right) > 0;

    public static bool operator >=(AvailableAggregatingPoolingName left, AvailableAggregatingPoolingName right)
        => left.CompareTo(right) >= 0;
}
