using DotWrap;
using System.Collections.Generic;

namespace DotWrap.TestLib;

[DotWrapExpose]
public class IReadOnlyListTestCases
{
    public static IReadOnlyList<int> GetReadOnlyListFromArray() => [10, 20, 30, 40, 50];
    
    public static IReadOnlyList<string> GetReadOnlyListFromList()
    {
        var list = new List<string> { "apple", "banana", "cherry" };
        return list.AsReadOnly();
    }
    
    public static bool TestReadOnlyListCount(IReadOnlyList<int> list)
    {
        return list.Count > 0;
    }
    
    public static int TestReadOnlyListIndexer(IReadOnlyList<int> list, int index)
    {
        return list[index];
    }
    
    public static bool TestReadOnlyListContains(IReadOnlyList<int> list, int item)
    {
        return list.Contains(item);
    }
}