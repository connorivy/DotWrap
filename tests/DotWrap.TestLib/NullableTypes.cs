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

// [DotWrapExpose]
public class CustomClass { }
