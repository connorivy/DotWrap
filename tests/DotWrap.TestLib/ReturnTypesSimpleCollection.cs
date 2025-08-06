using System.Runtime.InteropServices;
using DotWrap;
using DotWrap.Utils;

// [assembly: DotWrapExternalExpose(typeof(IList<>))]
[assembly: DotWrapExternalMethodMeta(typeof(IList<>), nameof(IList<>.Add), alias: "CustomAddName")]
[assembly: DotWrapExternalMethodMeta(typeof(IList<>), nameof(IList<>.Remove))]
[assembly: DotWrapExternalPropertyMeta(typeof(ICollection<>), nameof(ICollection<>.Count))]
[assembly: DotWrapExternalPropertyMeta(
    typeof(IReadOnlyCollection<>),
    nameof(IReadOnlyCollection<>.Count)
)]
[assembly: DotWrapExternalPropertyMeta(typeof(IDictionary<,>), nameof(IDictionary<,>.Keys))]
[assembly: DotWrapExternalPropertyMeta(typeof(KeyValuePair<,>), nameof(KeyValuePair<,>.Key))]
[assembly: DotWrapExternalPropertyMeta(typeof(KeyValuePair<,>), nameof(KeyValuePair<,>.Value))]
[assembly: DotWrapExternalPropertyMeta(typeof(System.Array), nameof(System.Array.Length))]
[assembly: DotWrapExternalIndexerMeta(typeof(System.Array))]
[assembly: DotWrapExternalMethodMeta(typeof(System.Array), "Add", ignore: true)]
[assembly: DotWrapExternalMethodMeta(typeof(System.Array), "Remove", ignore: true)]
[assembly: DotWrapExternalPropertyMeta(typeof(System.Array), "Count", PropertyType.None)]

namespace DotWrap.TestLib;

[DotWrapExpose]
public class ReturnTypesSimpleCollection
{
    public static int[] GetInt32Array() => new[] { int.MaxValue, int.MinValue };

    // [DotWrapMeta(alias: "Int32List")]
    public static List<int> GetInt32List() => [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    public static IReadOnlyList<int> GetReadOnlyInt32List() => [0, 1, 2, 3, 4, 5];

    // public static List<string> GetStringList() => new() { "Hello", "World" };
    public static Dictionary<int, string> GetIntStringDictionary()
    {
        return new Dictionary<int, string>
        {
            { 1, "One" },
            { 2, "Two" },
            { 3, "Three" },
        };
    }

    public static Dictionary<int, long> GetIntLongDictionary()
    {
        return new Dictionary<int, long>
        {
            { 1, 10000000000 },
            { 2, 20000000000 },
            { 3, 30000000000 },
        };
    }

    public static List<double> GetDoubleList() => new() { double.MaxValue, double.MinValue };

    public static List<long> GetLongList() => new() { long.MaxValue, long.MinValue };

    // public static uint[] GetUInt32Array() => new[] { uint.MaxValue, uint.MinValue };

    // public static long[] GetInt64Array() => new[] { long.MaxValue, long.MinValue };

    // public static ulong[] GetUInt64Array() => new[] { ulong.MaxValue, ulong.MinValue };

    // public static float[] GetSingleArray() => new[] { float.MaxValue, float.MinValue };

    // public static double[] GetDoubleArray() => new[] { double.MaxValue, double.MinValue };
}

[StructLayout(LayoutKind.Sequential)]
public struct ArrayInfo
{
    public IntPtr Ptr;
    public int Length;
}

public class ICollectionConfig : DotWrapPythonTypeConfig
{
    public override Type TypeToConfigure => typeof(ICollection<>);

    public override void ConfigureClassBody(
        Type matchingType,
        IndentedStringBuilder? genericClassBodyBuilder
    )
    {
        genericClassBodyBuilder?.AppendLine(
            @"
def __await__(self):
    return self._poll().__await__()

async def _poll(self):
    while True:
        if self.is_completed_successfully:
            return self.result
        elif self.is_faulted:
            raise RuntimeError(""Error polling task"")
        await asyncio.sleep(0.1)
        "
        );
    }
}
