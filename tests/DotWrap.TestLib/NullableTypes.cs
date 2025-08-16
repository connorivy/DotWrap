using DotWrap;
using DotWrap.TestLib;

[assembly: DotWrapExternalMethodMeta(typeof(System.Nullable<>), ".ctor", [typeof(Type)])]
[assembly: DotWrapExternalPropertyMeta(typeof(System.Nullable<>), nameof(Nullable<int>.HasValue))]
[assembly: DotWrapExternalPropertyMeta(typeof(System.Nullable<>), nameof(Nullable<int>.Value))]

namespace DotWrap.TestLib;

[DotWrapExpose]
public static class NullableTypes
{
    public static int? NullableInt(int? value)
    {
        return value;
    }

    public static string? NullableString(string? value)
    {
        return value;
    }

    public static CustomClass? NullableCustomClass(CustomClass? value)
    {
        return value;
    }
}

[DotWrapExpose]
public class CustomClass { }
