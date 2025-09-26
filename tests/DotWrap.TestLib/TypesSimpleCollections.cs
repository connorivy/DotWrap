namespace DotWrap.TestLib;

[DotWrapExpose]
public class TypesSimpleCollections
{
    public static Dictionary<string, string>? DictionaryOfStringAndString(Dictionary<string, string>? items)
    {
        return items;
    }
}
