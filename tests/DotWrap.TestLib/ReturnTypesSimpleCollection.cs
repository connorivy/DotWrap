using System.Runtime.InteropServices;
using DotWrap;
using DotWrap.Configuration;
using DotWrap.Internal;
using DotWrap.MSBuild;
using DotWrap.MSBuild.WrapperGenerators.Python.Builders;
// using DotWrap.MSBuild.WrapperGenerators.Python;
// using DotWrap.MSBuild.WrapperGenerators.Python.Builders;
using DotWrap.Utils;
using DotWrap.Utils.Python;
using static DotWrap.Internal.Constants;
using static DotWrap.Utils.PythonConstants;

// using static DotWrap.MSBuild.WrapperGenerators.Python.PythonConstants;

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

    public override void ConfigureGenericClassBody(
        ExportedTypeDefinitionInfo exportedType,
        Type matchingType,
        IndentedStringBuilder genericClassBodyBuilder
    )
    {
        var assemblyName =
            matchingType.GenericTypeArguments[0].FullName
            ?? matchingType.GenericTypeArguments[0].Name;

        var originalTypeString = DotWrapUtils.GetOriginalTypeString(assemblyName);
        var genericArg = PythonNamingUtils.MapTypeToPython(originalTypeString);
        genericClassBodyBuilder?.AppendLine(
            $@"
def to_list(self) -> list[""{genericArg}""]:
    pass
        "
        );
    }

    public override void ConfigureClassBody(
        ExportedTypeDefinitionInfo typeInfo,
        Type matchingType,
        IndentedStringBuilder classBody
    )
    {
        var assemblyName =
            matchingType.GenericTypeArguments[0].FullName
            ?? matchingType.GenericTypeArguments[0].Name;

        var originalTypeString = DotWrapUtils.GetOriginalTypeString(assemblyName);
        var genericArg = PythonNamingUtils.MapTypeToPython(originalTypeString);
        var exposedType = DotWrapUtils.GetExposedTypeFromCsType(
            genericArg,
            out bool isOriginalType
        );
        classBody.AppendLine($"def to_list(self) -> list[\"{genericArg}\"]:");
        using var indent1 = classBody.IndentUntilDispose();

        var numpyType = PythonNamingUtils.MapTypeToNumpy(genericArg);
        classBody.AppendLine(
            @$"
""""""
Converts the array data to a list of the specified dtype.
""""""
length = {Lib}.{typeInfo.EntryPrefix}{GetCount}(self.{Ptr})
arr = np.empty(length, dtype={numpyType})

# get stable pointer to the array data
arr_ptr = _dotwrap_ffi.cast(""int*"", _dotwrap_ffi.from_buffer(arr))
{Lib}.{typeInfo.EntryPrefix}{FillArr}(self.{Ptr}, arr_ptr, length)
        "
        );

        if (isOriginalType)
        {
            classBody.AppendLine("return arr.tolist()");
        }
        else
        {
            OriginalAndExposedTypeInfo genericTypeInfo = new(
                originalTypeString,
                isOriginalType ? null : exposedType
            );

            var externalTypeAssignment = CffiApiMethodBuilder.GetExternalResultAssignment(
                genericTypeInfo
            );
            classBody.AppendLine("final_list = []");
            using (var forBlock = classBody.AppendLineWithNewBlock("for i in range(length):"))
            {
                if (numpyType == "np.intp")
                {
                    classBody.AppendLine($"{InternalPyResult} = {Ffi}.cast('void *', arr[i])");
                }
                else
                {
                    classBody.AppendLine($"{InternalPyResult} = arr[i]");
                }
                if (externalTypeAssignment != null)
                {
                    classBody.AppendLine($"{externalTypeAssignment}");
                    classBody.AppendLine($"final_list.append({ExportedPyResult})");
                }
                else
                {
                    classBody.AppendLine($"final_list.append({InternalPyResult})");
                }
            }
            classBody.AppendLine("return final_list");
        }
    }
}

public class IReadOnlyCollectionConfig : ICollectionConfig
{
    public override Type TypeToConfigure => typeof(IReadOnlyCollection<>);
}
