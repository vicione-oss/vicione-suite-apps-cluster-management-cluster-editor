using System;
using System.ComponentModel;

namespace ViciOne.Ui.ClusterEditor.Components.UniversalInput.Extensions;

public static class TypeExtensions
{
    public static Type GetTypeWithoutNullability(this Type type)
        => type.IsNullable() ? new NullableConverter(type).UnderlyingType : type;

    public static bool IsNullable(this Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

    public static bool IsNumeric(this Type type)
        => Type.GetTypeCode(GetTypeWithoutNullability(type))
                is TypeCode.Byte
                or TypeCode.SByte
                or TypeCode.UInt16
                or TypeCode.UInt32
                or TypeCode.UInt64
                or TypeCode.Int16
                or TypeCode.Int32
                or TypeCode.Int64
                or TypeCode.Decimal
                or TypeCode.Double
                or TypeCode.Single;
}
