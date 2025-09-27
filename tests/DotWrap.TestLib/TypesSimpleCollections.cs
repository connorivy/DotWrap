namespace DotWrap.TestLib;

[DotWrapExpose]
public class TypesSimpleCollections
{
    public static Dictionary<string, string>? DictionaryOfStringAndString(Dictionary<string, string>? items)
    {
        return items;
    }

    public static IEnumerable<KeyValuePair<int, double>>? IEnumerableOfKvpOfIntAndDouble(IEnumerable<KeyValuePair<int, double>>? items)
    {
        return items;
    }

    public static List<string> ListOfString(List<string> items)
    {
        return items;
    }

    public static List<string?> ListOfNullableString(List<string?> items)
    {
        return items;
    }

    public static List<string>? NullableListOfString(List<string>? items)
    {
        return items;
    }

    public static List<string?>? NullableListOfNullableString(List<string?>? items)
    {
        return items;
    }


    public static string[] ArrayOfString(string[] items)
    {
        return items;
    }

    public static string?[] ArrayOfNullableString(string?[] items)
    {
        return items;
    }

    public static string[]? NullableArrayOfString(string[]? items)
    {
        return items;
    }

    public static string?[]? NullableArrayOfNullableString(string?[]? items)
    {
        return items;
    }

    public static int[] ArrayOfInt(int[] items)
    {
        return items;
    }

    public static int?[] ArrayOfNullableInt(int?[] items)
    {
        return items;
    }

    public static int[]? NullableArrayOfInt(int[]? items)
    {
        return items;
    }

    public static int?[]? NullableArrayOfNullableInt(int?[]? items)
    {
        return items;
    }
}
