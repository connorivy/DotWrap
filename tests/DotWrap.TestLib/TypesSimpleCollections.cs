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

    public static string[] ArrayOfString(string[] items)
    {
        return items;
    }

    public static string?[] ArrayOfNullableString(string?[] items)
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
}
